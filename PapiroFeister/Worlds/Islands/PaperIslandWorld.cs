using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PapiroFeister.Textures.Generators;
using PapiroFeister.Worlds.Objects;

namespace PapiroFeister.Worlds.Islands;

public sealed class PaperIslandWorld : System.IDisposable
{
    private const float OceanLevel = -0.08f;
    private const int WaveSegments = 10;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly BasicEffect _surfaceEffect;
    private readonly BasicEffect _borderEffect;
    private readonly BasicEffect _waveEffect;
    private readonly BasicEffect _objectEffect;
    private readonly Texture2D _paperTexture;
    private readonly Random _random;

    private readonly VertexPositionNormalTexture[] _oceanVertices;
    private readonly VertexPositionNormalTexture[] _landVertices;
    private readonly short[] _landIndices;
    private readonly VertexPositionColor[] _borderVertices;
    private readonly short[] _borderIndices;
    private readonly VertexPositionColor[] _rockVertices;
    private readonly short[] _rockIndices;
    private readonly List<WorldObject> _worldObjects = [];

    private readonly Vector2[] _coastPoints;
    private readonly float[] _coastHeights;

    private readonly List<Wave> _waves = [];
    private readonly VertexPositionColor[] _waveVertices = new VertexPositionColor[(WaveSegments + 1) * 2];
    private readonly short[] _waveIndices = CreateWaveIndices(WaveSegments);
    private float _waveSpawnAccumulator;
    private float _lastDrawTimeSeconds;
    private bool _drawTimeInitialized;

    private readonly short[] _quadIndices = [0, 1, 2, 2, 1, 3];

    public float PlayableHalfSize { get; }

    public PaperIslandWorld(GraphicsDevice graphicsDevice, float playableHalfSize = 24f, float oceanHalfSize = 80f)
    {
        _graphicsDevice = graphicsDevice;
        PlayableHalfSize = MathHelper.Max(playableHalfSize, 8f);
        oceanHalfSize = MathHelper.Max(oceanHalfSize, PlayableHalfSize + 6f);
        _random = new Random();

        _paperTexture = PaperTextureGenerator.GenerateTextureWithoutDots(graphicsDevice);

        _surfaceEffect = new BasicEffect(graphicsDevice)
        {
            TextureEnabled = true,
            Texture = _paperTexture,
            LightingEnabled = false,
            VertexColorEnabled = false,
            DiffuseColor = Color.White.ToVector3()
        };

        _borderEffect = new BasicEffect(graphicsDevice)
        {
            TextureEnabled = false,
            LightingEnabled = false,
            VertexColorEnabled = true,
            DiffuseColor = Color.White.ToVector3(),
            Alpha = 1f
        };

        _waveEffect = new BasicEffect(graphicsDevice)
        {
            TextureEnabled = false,
            LightingEnabled = false,
            VertexColorEnabled = true,
            DiffuseColor = Color.White.ToVector3(),
            Alpha = 1f
        };

        _objectEffect = new BasicEffect(graphicsDevice)
        {
            TextureEnabled = false,
            LightingEnabled = false,
            VertexColorEnabled = true,
            DiffuseColor = Color.White.ToVector3(),
            Alpha = 1f
        };

        _oceanVertices = CreateQuad(oceanHalfSize, y: OceanLevel, uvScale: 1.05f);

        (Vector2[] baseCoastPoints, float[] baseCoastHeights) = GenerateCoastline(PlayableHalfSize);
        (_coastPoints, _coastHeights) = ResampleAndSmoothCoastline(baseCoastPoints, baseCoastHeights, targetCount: 240);
        (_landVertices, _landIndices) = BuildLandMesh(_coastPoints, _coastHeights);
        (_borderVertices, _borderIndices) = BuildBorderMesh(_coastPoints, _coastHeights, thickness: 0.17f);
        (_rockVertices, _rockIndices) = BuildRocks(_coastPoints, minDistance: 1.1f, maxDistance: 5.6f);

        InitializeWorldObjects();
    }

    public void Draw(Matrix view, Matrix projection, Vector3 cameraPosition, float totalTimeSeconds)
    {
        UpdateWaves(totalTimeSeconds);

        RasterizerState previousRasterizer = _graphicsDevice.RasterizerState;
        BlendState previousBlend = _graphicsDevice.BlendState;
        DepthStencilState previousDepthStencil = _graphicsDevice.DepthStencilState;

        _graphicsDevice.RasterizerState = new RasterizerState { CullMode = CullMode.None };
        _graphicsDevice.BlendState = BlendState.Opaque;
        _graphicsDevice.DepthStencilState = DepthStencilState.Default;

        _surfaceEffect.World = Matrix.Identity;
        _surfaceEffect.View = view;
        _surfaceEffect.Projection = projection;

        _surfaceEffect.DiffuseColor = new Vector3(0.12f, 0.43f, 0.78f);
        DrawTexturedQuad(_oceanVertices);

        _surfaceEffect.DiffuseColor = Color.White.ToVector3();
        DrawLandMesh();

        _borderEffect.World = Matrix.Identity;
        _borderEffect.View = view;
        _borderEffect.Projection = projection;

        foreach (EffectPass pass in _borderEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            if (_rockVertices.Length > 0)
            {
                _graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    _rockVertices,
                    0,
                    _rockVertices.Length,
                    _rockIndices,
                    0,
                    _rockIndices.Length / 3);
            }

            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _borderVertices,
                0,
                _borderVertices.Length,
                _borderIndices,
                0,
                _borderIndices.Length / 3);
        }

        DrawWaves(view, projection);
        DrawDecorations(view, projection, cameraPosition);

        _graphicsDevice.DepthStencilState = previousDepthStencil;
        _graphicsDevice.BlendState = previousBlend;
        _graphicsDevice.RasterizerState = previousRasterizer;
    }

    public void Dispose()
    {
        _surfaceEffect.Dispose();
        _borderEffect.Dispose();
        _waveEffect.Dispose();
        _objectEffect.Dispose();
        foreach (WorldObject worldObject in _worldObjects)
            worldObject.Dispose();
        _paperTexture.Dispose();
    }

    private void DrawTexturedQuad(VertexPositionNormalTexture[] vertices)
    {
        foreach (EffectPass pass in _surfaceEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                vertices,
                0,
                4,
                _quadIndices,
                0,
                2);
        }
    }

    private void DrawLandMesh()
    {
        foreach (EffectPass pass in _surfaceEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _landVertices,
                0,
                _landVertices.Length,
                _landIndices,
                0,
                _landIndices.Length / 3);
        }
    }

    private void DrawWaves(Matrix view, Matrix projection)
    {
        if (_waves.Count == 0)
            return;

        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

        _waveEffect.World = Matrix.Identity;
        _waveEffect.View = view;
        _waveEffect.Projection = projection;

        foreach (Wave wave in _waves)
        {
            BuildWaveStrip(wave, _waveVertices);

            foreach (EffectPass pass in _waveEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    _waveVertices,
                    0,
                    _waveVertices.Length,
                    _waveIndices,
                    0,
                    _waveIndices.Length / 3);
            }
        }
    }

    private void DrawDecorations(Matrix view, Matrix projection, Vector3 cameraPosition)
    {
        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.DepthStencilState = DepthStencilState.Default;

        foreach (WorldObject worldObject in _worldObjects)
            worldObject.Draw(_graphicsDevice, _objectEffect, view, projection, cameraPosition);
    }

    private void InitializeWorldObjects()
    {
        _worldObjects.Add(new FenceObject(initialPosition: Vector3.Zero, halfSize: PlayableHalfSize));

        float ring = MathF.Max(4f, PlayableHalfSize - 2.8f);
        const float y = 0.08f;
        Vector3[] treePositions =
        [
            new Vector3(-ring, y, -ring * 0.35f),
            new Vector3(-ring * 0.22f, y, -ring),
            new Vector3(ring * 0.52f, y, -ring),
            new Vector3(ring, y, -ring * 0.18f),
            new Vector3(ring, y, ring * 0.42f),
            new Vector3(ring * 0.35f, y, ring),
            new Vector3(-ring * 0.58f, y, ring),
            new Vector3(-ring, y, ring * 0.2f)
        ];

        foreach (Vector3 position in treePositions)
            _worldObjects.Add(new TreeObject(position));
    }

    private static VertexPositionNormalTexture[] CreateQuad(float halfSize, float y, float uvScale)
    {
        Vector3 normal = Vector3.Up;

        return
        [
            new VertexPositionNormalTexture(new Vector3(-halfSize, y, -halfSize), normal, new Vector2(0f, uvScale)),
            new VertexPositionNormalTexture(new Vector3(halfSize, y, -halfSize), normal, new Vector2(uvScale, uvScale)),
            new VertexPositionNormalTexture(new Vector3(-halfSize, y, halfSize), normal, new Vector2(0f, 0f)),
            new VertexPositionNormalTexture(new Vector3(halfSize, y, halfSize), normal, new Vector2(uvScale, 0f))
        ];
    }

    private (Vector2[] coastPoints, float[] coastHeights) GenerateCoastline(float playableHalfSize)
    {
        int total = 160;

        Vector2[] points = new Vector2[total];
        float[] heights = new float[total];

        // Keep border close to the walkable zone so the player can reach the coast visually.
        float minimumHalfSpan = playableHalfSize + 1.8f;
        float coastHalfX = minimumHalfSpan + NextFloat(0.7f, 2.4f);
        float coastHalfZ = minimumHalfSpan + NextFloat(0.5f, 2.1f);

        float phaseA = NextFloat(0f, MathHelper.TwoPi);
        float phaseB = NextFloat(0f, MathHelper.TwoPi);
        float phaseC = NextFloat(0f, MathHelper.TwoPi);

        int[] rampCenters =
        [
            _random.Next(total),
            _random.Next(total),
            _random.Next(total)
        ];
        int rampWidth = Math.Max(8, total / 16);

        for (int index = 0; index < total; index++)
        {
            float angle = (index / (float)total) * MathHelper.TwoPi;
            float ca = MathF.Cos(angle);
            float sa = MathF.Sin(angle);

            // Squircle-like base avoids long straight segments while preserving a broad island footprint.
            float powX = MathF.Pow(MathF.Abs(ca), 0.72f);
            float powY = MathF.Pow(MathF.Abs(sa), 0.72f);

            Vector2 basePoint = new Vector2(
                MathF.Sign(ca) * powX * coastHalfX,
                MathF.Sign(sa) * powY * coastHalfZ);

            Vector2 outward = SafeNormalize2D(new Vector2(basePoint.X / coastHalfX, basePoint.Y / coastHalfZ), new Vector2(ca, sa));

            float lowFrequencyShape =
                (MathF.Sin((angle * 2.3f) + phaseA) * 1.2f) +
                (MathF.Sin((angle * 3.7f) + phaseB) * 0.85f) +
                (MathF.Sin((angle * 6.2f) + phaseC) * 0.45f);

            float localNoise = NextFloat(-0.45f, 0.45f);
            float coastOffset = lowFrequencyShape + localNoise;
            Vector2 point = basePoint + (outward * coastOffset);

            // Keep the natural coast outside the walkable square while preserving curved edges.
            float shorelineMargin = 0.7f;
            point = PushPointOutsideWalkableSquare(point, outward, playableHalfSize + shorelineMargin);

            float rampWeight = 0f;
            for (int r = 0; r < rampCenters.Length; r++)
            {
                int distance = CircularDistance(index, rampCenters[r], total);
                float normalized = 1f - MathHelper.Clamp(distance / (float)rampWidth, 0f, 1f);
                rampWeight = Math.Max(rampWeight, normalized * normalized * (3f - 2f * normalized));
            }

            float baseHeight = MathHelper.Lerp(0.045f, 0.1f, NextFloat(0f, 1f));
            float rampAdjusted = MathHelper.Lerp(baseHeight, 0.024f, rampWeight);

            points[index] = point;
            heights[index] = rampAdjusted;
        }

        return (points, heights);
    }

    private (VertexPositionNormalTexture[] vertices, short[] indices) BuildLandMesh(Vector2[] coastPoints, float[] coastHeights)
    {
        int count = coastPoints.Length;
        const int radialRings = 8;
        VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[1 + (count * radialRings)];
        List<short> indices = new List<short>(count * radialRings * 6);

        float centerHeight = 0.42f + NextFloat(0f, 0.08f);
        vertices[0] = new VertexPositionNormalTexture(
            new Vector3(0f, centerHeight, 0f),
            Vector3.Up,
            new Vector2(0.5f, 0.5f));

        for (int ring = 1; ring <= radialRings; ring++)
        {
            float t = ring / (float)radialRings;
            float shapeT = MathF.Pow(t, 0.86f);
            float domeHeight = centerHeight * (1f - t * t);

            for (int i = 0; i < count; i++)
            {
                Vector2 coast = coastPoints[i];
                float shoreHeight = coastHeights[i];

                Vector2 pos2D = coast * shapeT;
                float microVariation = 0.012f * MathF.Sin((i * 0.31f) + (t * 8.2f));
                float height = (shoreHeight * t) + domeHeight + microVariation * MathF.Sqrt(1f - t);

                float u = (pos2D.X / (PlayableHalfSize * 3.2f)) + 0.5f;
                float v = (pos2D.Y / (PlayableHalfSize * 3.2f)) + 0.5f;

                int vertexIndex = 1 + ((ring - 1) * count) + i;
                vertices[vertexIndex] = new VertexPositionNormalTexture(
                    new Vector3(pos2D.X, height, pos2D.Y),
                    Vector3.Up,
                    new Vector2(u, v));
            }
        }

        // Center fan to first ring.
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;

            indices.Add(0);
            indices.Add((short)(1 + i));
            indices.Add((short)(1 + next));
        }

        // Stitch all radial rings.
        for (int ring = 0; ring < radialRings - 1; ring++)
        {
            int ringStart = 1 + (ring * count);
            int nextRingStart = ringStart + count;

            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;

                short a = (short)(ringStart + i);
                short b = (short)(ringStart + next);
                short c = (short)(nextRingStart + i);
                short d = (short)(nextRingStart + next);

                indices.Add(a);
                indices.Add(c);
                indices.Add(b);

                indices.Add(b);
                indices.Add(c);
                indices.Add(d);
            }
        }

        return (vertices, indices.ToArray());
    }

    private (VertexPositionColor[] vertices, short[] indices) BuildBorderMesh(Vector2[] coastPoints, float[] coastHeights, float thickness)
    {
        int count = coastPoints.Length;
        VertexPositionColor[] vertices = new VertexPositionColor[count * 2];
        List<short> indices = new List<short>(count * 6);

        for (int i = 0; i < count; i++)
        {
            Vector2 p = coastPoints[i];
            Vector2 outward = ComputeOutwardNormal(i, coastPoints);
            Vector2 outer = p + outward * thickness;

            float baseHeight = coastHeights[i] + 0.012f;
            vertices[i * 2] = new VertexPositionColor(new Vector3(p.X, baseHeight, p.Y), Color.Black);
            vertices[(i * 2) + 1] = new VertexPositionColor(new Vector3(outer.X, baseHeight - 0.006f, outer.Y), Color.Black);
        }

        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;

            short innerA = (short)(i * 2);
            short outerA = (short)(i * 2 + 1);
            short innerB = (short)(next * 2);
            short outerB = (short)(next * 2 + 1);

            indices.Add(innerA);
            indices.Add(innerB);
            indices.Add(outerA);

            indices.Add(innerB);
            indices.Add(outerB);
            indices.Add(outerA);
        }

        return (vertices, indices.ToArray());
    }

    private (VertexPositionColor[] vertices, short[] indices) BuildRocks(Vector2[] coastPoints, float minDistance, float maxDistance)
    {
        int rockCount = 22 + _random.Next(16);
        List<VertexPositionColor> vertices = new List<VertexPositionColor>(rockCount * 9);
        List<short> indices = new List<short>(rockCount * 18);

        for (int i = 0; i < rockCount; i++)
        {
            int coastIndex = _random.Next(coastPoints.Length);
            Vector2 coast = coastPoints[coastIndex];
            Vector2 outward = ComputeOutwardNormal(coastIndex, coastPoints);

            float distance = NextFloat(minDistance, maxDistance);
            Vector2 center2D = coast + outward * distance;
            float radius = NextFloat(0.18f, 0.62f);
            int segments = 6;

            float centerY = OceanLevel + NextFloat(0.01f, 0.06f);
            Color rockColor = new Color(45, 47, 52, 190);

            short centerIndex = (short)vertices.Count;
            vertices.Add(new VertexPositionColor(new Vector3(center2D.X, centerY, center2D.Y), rockColor));

            for (int s = 0; s < segments; s++)
            {
                float angle = (MathHelper.TwoPi * s / segments) + NextFloat(-0.16f, 0.16f);
                float r = radius * NextFloat(0.72f, 1.08f);
                Vector2 rim = center2D + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * r;
                float y = centerY + NextFloat(0f, 0.055f);
                vertices.Add(new VertexPositionColor(new Vector3(rim.X, y, rim.Y), rockColor));
            }

            for (int s = 0; s < segments; s++)
            {
                short a = (short)(centerIndex + 1 + s);
                short b = (short)(centerIndex + 1 + ((s + 1) % segments));

                indices.Add(centerIndex);
                indices.Add(a);
                indices.Add(b);
            }
        }

        return (vertices.ToArray(), indices.ToArray());
    }

    private void UpdateWaves(float totalTimeSeconds)
    {
        if (!_drawTimeInitialized)
        {
            _lastDrawTimeSeconds = totalTimeSeconds;
            _drawTimeInitialized = true;
            return;
        }

        float dt = MathHelper.Clamp(totalTimeSeconds - _lastDrawTimeSeconds, 0f, 0.05f);
        _lastDrawTimeSeconds = totalTimeSeconds;

        for (int i = _waves.Count - 1; i >= 0; i--)
        {
            Wave wave = _waves[i];
            wave.Age += dt;
            if (wave.Age >= wave.Lifetime)
                _waves.RemoveAt(i);
            else
                _waves[i] = wave;
        }

        _waveSpawnAccumulator += dt;
        float spawnInterval = 0.16f;

        while (_waveSpawnAccumulator >= spawnInterval)
        {
            _waveSpawnAccumulator -= spawnInterval;
            SpawnWave();
        }
    }

    private void SpawnWave()
    {
        if (_waves.Count >= 64 || _coastPoints.Length < 2)
            return;

        int edge = _random.Next(_coastPoints.Length);
        Wave wave = new Wave
        {
            CoastParam = edge + NextFloat(0.05f, 0.95f),
            Age = 0f,
            Lifetime = NextFloat(2.1f, 3.8f),
            Width = NextFloat(0.07f, 0.14f),
            Span = NextFloat(5f, 12f),
            StartDistance = NextFloat(2.4f, 4.7f),
            EndDistance = NextFloat(0.35f, 0.9f)
        };

        _waves.Add(wave);
    }

    private void BuildWaveStrip(Wave wave, VertexPositionColor[] output)
    {
        float progress = wave.Age / wave.Lifetime;
        float distance = MathHelper.Lerp(wave.StartDistance, wave.EndDistance, progress);

        float alphaEnvelope = MathF.Sin(progress * MathF.PI);
        float baseAlpha = 0.22f * MathHelper.Clamp(alphaEnvelope, 0f, 1f);
        float y = OceanLevel + 0.014f;

        for (int s = 0; s <= WaveSegments; s++)
        {
            float along01 = s / (float)WaveSegments;
            float signedAlong = (along01 - 0.5f) * wave.Span;

            SampleCoast(wave.CoastParam + signedAlong, out Vector2 coastPoint, out Vector2 tangent);
            Vector2 towardIsland = SafeNormalize2D(-coastPoint, new Vector2(-tangent.Y, tangent.X));
            Vector2 center = coastPoint - towardIsland * distance;

            float edgeFade = MathF.Sin(along01 * MathF.PI);
            float alpha = baseAlpha * (0.4f + 0.6f * edgeFade);
            Color waveColor = new Color(1f, 1f, 1f, alpha);

            float width = wave.Width * (0.85f + 0.15f * edgeFade);
            Vector2 offset = towardIsland * width;

            output[s * 2] = new VertexPositionColor(new Vector3(center.X - offset.X, y, center.Y - offset.Y), waveColor);
            output[s * 2 + 1] = new VertexPositionColor(new Vector3(center.X + offset.X, y, center.Y + offset.Y), waveColor);
        }
    }

    private void SampleCoast(float param, out Vector2 point, out Vector2 tangent)
    {
        int count = _coastPoints.Length;
        float wrapped = Wrap(param, count);
        int i0 = (int)MathF.Floor(wrapped);
        int i1 = (i0 + 1) % count;
        float t = wrapped - i0;

        Vector2 a = _coastPoints[i0];
        Vector2 b = _coastPoints[i1];

        point = Vector2.Lerp(a, b, t);
        tangent = SafeNormalize2D(b - a, Vector2.UnitX);
    }

    private (Vector2[] points, float[] heights) ResampleAndSmoothCoastline(Vector2[] points, float[] heights, int targetCount)
    {
        int sourceCount = points.Length;
        float[] cumulative = new float[sourceCount + 1];

        for (int i = 0; i < sourceCount; i++)
        {
            int next = (i + 1) % sourceCount;
            cumulative[i + 1] = cumulative[i] + Vector2.Distance(points[i], points[next]);
        }

        float perimeter = cumulative[sourceCount];
        Vector2[] resampledPoints = new Vector2[targetCount];
        float[] resampledHeights = new float[targetCount];

        for (int i = 0; i < targetCount; i++)
        {
            float targetDistance = (i / (float)targetCount) * perimeter;
            int segment = 0;

            while (segment < sourceCount - 1 && cumulative[segment + 1] < targetDistance)
                segment++;

            int next = (segment + 1) % sourceCount;
            float segmentStart = cumulative[segment];
            float segmentLength = Math.Max(cumulative[segment + 1] - segmentStart, 0.0001f);
            float t = (targetDistance - segmentStart) / segmentLength;

            resampledPoints[i] = Vector2.Lerp(points[segment], points[next], t);
            resampledHeights[i] = MathHelper.Lerp(heights[segment], heights[next], t);
        }

        // A light smoothing pass avoids faceted transitions while preserving random silhouette.
        for (int pass = 0; pass < 2; pass++)
        {
            Vector2[] smoothPoints = new Vector2[targetCount];
            float[] smoothHeights = new float[targetCount];

            for (int i = 0; i < targetCount; i++)
            {
                int prev = (i - 1 + targetCount) % targetCount;
                int next = (i + 1) % targetCount;

                smoothPoints[i] = (resampledPoints[prev] * 0.2f) + (resampledPoints[i] * 0.6f) + (resampledPoints[next] * 0.2f);
                smoothHeights[i] = (resampledHeights[prev] * 0.2f) + (resampledHeights[i] * 0.6f) + (resampledHeights[next] * 0.2f);
            }

            resampledPoints = smoothPoints;
            resampledHeights = smoothHeights;
        }

        return (resampledPoints, resampledHeights);
    }

    private static short[] CreateWaveIndices(int segments)
    {
        short[] indices = new short[segments * 6];
        int write = 0;

        for (int s = 0; s < segments; s++)
        {
            short a = (short)(s * 2);
            short b = (short)(s * 2 + 1);
            short c = (short)(s * 2 + 2);
            short d = (short)(s * 2 + 3);

            indices[write++] = a;
            indices[write++] = c;
            indices[write++] = b;

            indices[write++] = b;
            indices[write++] = c;
            indices[write++] = d;
        }

        return indices;
    }

    private static Vector2 ComputeOutwardNormal(int index, Vector2[] coastPoints)
    {
        int count = coastPoints.Length;
        int prev = (index - 1 + count) % count;
        int next = (index + 1) % count;

        Vector2 tangent = SafeNormalize2D(coastPoints[next] - coastPoints[prev], Vector2.UnitX);
        Vector2 outward = new Vector2(tangent.Y, -tangent.X);

        if (Vector2.Dot(outward, coastPoints[index]) < 0f)
            outward *= -1f;

        return SafeNormalize2D(outward, SafeNormalize2D(coastPoints[index], Vector2.UnitX));
    }

    private static float Wrap(float value, int length)
    {
        float wrapped = value % length;
        return wrapped < 0f ? wrapped + length : wrapped;
    }

    private float NextFloat(float min, float max)
    {
        return min + (float)_random.NextDouble() * (max - min);
    }

    private static int CircularDistance(int a, int b, int size)
    {
        int diff = Math.Abs(a - b);
        return Math.Min(diff, size - diff);
    }

    private static Vector2 PushPointOutsideWalkableSquare(Vector2 point, Vector2 outward, float minimumAbs)
    {
        bool alreadyOutside = MathF.Abs(point.X) >= minimumAbs || MathF.Abs(point.Y) >= minimumAbs;
        if (alreadyOutside)
            return point;

        Vector2 safeOutward = SafeNormalize2D(outward, SafeNormalize2D(point, Vector2.UnitX));

        float pushX = float.PositiveInfinity;
        float pushY = float.PositiveInfinity;

        float absOutX = MathF.Abs(safeOutward.X);
        if (absOutX > 0.0001f)
            pushX = (minimumAbs - MathF.Abs(point.X)) / absOutX;

        float absOutY = MathF.Abs(safeOutward.Y);
        if (absOutY > 0.0001f)
            pushY = (minimumAbs - MathF.Abs(point.Y)) / absOutY;

        float pushDistance = MathF.Min(pushX, pushY);
        if (float.IsInfinity(pushDistance) || pushDistance < 0f)
            pushDistance = minimumAbs;

        return point + (safeOutward * (pushDistance + 0.04f));
    }

    private static Vector2 SafeNormalize2D(Vector2 value, Vector2 fallback)
    {
        if (value.LengthSquared() < 0.0001f)
            return fallback;

        value.Normalize();
        return value;
    }

    private struct Wave
    {
        public float CoastParam;
        public float Age;
        public float Lifetime;
        public float Width;
        public float Span;
        public float StartDistance;
        public float EndDistance;
    }
}
