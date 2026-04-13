using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PapiroFeister.Worlds.Objects;

public interface IObjectRenderer : IDisposable
{
    void Draw(
        GraphicsDevice graphicsDevice,
        BasicEffect effect,
        Vector3 worldPosition,
        Matrix view,
        Matrix projection,
        Vector3 cameraPosition);
}

public interface IObjectCollider
{
    bool IntersectsSphere(Vector3 worldPosition, Vector3 sphereCenter, float sphereRadius);
}

public abstract class WorldObject : IDisposable
{
    private readonly IObjectRenderer _renderer;
    private readonly IObjectCollider _collider;

    protected WorldObject(Vector3 initialPosition, IObjectRenderer renderer, IObjectCollider collider)
    {
        Position = initialPosition;
        _renderer = renderer;
        _collider = collider;
    }

    public Vector3 Position { get; private set; }

    public void SetPosition(Vector3 position)
    {
        Position = position;
    }

    public void Translate(Vector3 delta)
    {
        Position += delta;
    }

    public void Draw(GraphicsDevice graphicsDevice, BasicEffect effect, Matrix view, Matrix projection, Vector3 cameraPosition)
    {
        _renderer.Draw(graphicsDevice, effect, Position, view, projection, cameraPosition);
    }

    public bool IntersectsSphere(Vector3 sphereCenter, float sphereRadius)
    {
        return _collider.IntersectsSphere(Position, sphereCenter, sphereRadius);
    }

    public virtual void Dispose()
    {
        _renderer.Dispose();
    }
}

public abstract class CameraFacingObject : WorldObject
{
    protected CameraFacingObject(Vector3 initialPosition, IObjectRenderer renderer, IObjectCollider collider)
        : base(initialPosition, renderer, collider)
    {
    }
}

public abstract class WorldStaticObject : WorldObject
{
    protected WorldStaticObject(Vector3 initialPosition, IObjectRenderer renderer, IObjectCollider collider)
        : base(initialPosition, renderer, collider)
    {
    }
}

public sealed class SphereCollider : IObjectCollider
{
    private readonly Vector3 _centerOffset;
    private readonly float _radius;

    public SphereCollider(Vector3 centerOffset, float radius)
    {
        _centerOffset = centerOffset;
        _radius = radius;
    }

    public bool IntersectsSphere(Vector3 worldPosition, Vector3 sphereCenter, float sphereRadius)
    {
        Vector3 center = worldPosition + _centerOffset;
        float combinedRadius = _radius + sphereRadius;
        return Vector3.DistanceSquared(center, sphereCenter) <= combinedRadius * combinedRadius;
    }
}

public sealed class FenceRingCollider : IObjectCollider
{
    private readonly float _halfSize;
    private readonly float _thickness;

    public FenceRingCollider(float halfSize, float thickness)
    {
        _halfSize = halfSize;
        _thickness = thickness;
    }

    public bool IntersectsSphere(Vector3 worldPosition, Vector3 sphereCenter, float sphereRadius)
    {
        Vector3 localCenter = sphereCenter - worldPosition;
        float absX = MathF.Abs(localCenter.X);
        float absZ = MathF.Abs(localCenter.Z);

        float minBand = _halfSize - _thickness - sphereRadius;
        float maxBand = _halfSize + _thickness + sphereRadius;

        bool intersectXBand = absX >= minBand && absX <= maxBand && absZ <= _halfSize + sphereRadius;
        bool intersectZBand = absZ >= minBand && absZ <= maxBand && absX <= _halfSize + sphereRadius;

        return intersectXBand || intersectZBand;
    }
}
