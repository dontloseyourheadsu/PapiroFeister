using Microsoft.Xna.Framework;
using System;

namespace PapiroFeister.Cameras;

/// <summary>
/// A third-person camera that orbits around a target point with configurable distance and rotation.
/// </summary>
public class Camera3D
{
    // Camera matrices
    private Matrix _view;
    private Matrix _projection;

    // Camera parameters
    private Vector3 _targetPosition;
    private float _distance;
    private float _rotationX; // Pitch (up/down)
    private float _rotationY; // Yaw (left/right)

    // Camera position derived from parameters
    private Vector3 _position;

    /// <summary>
    /// Gets the current view matrix.
    /// </summary>
    public Matrix View => _view;

    /// <summary>
    /// Gets the projection matrix.
    /// </summary>
    public Matrix Projection => _projection;

    /// <summary>
    /// Gets the current camera position in world space.
    /// </summary>
    public Vector3 Position => _position;

    /// <summary>
    /// Gets the target position the camera is looking at.
    /// </summary>
    public Vector3 TargetPosition => _targetPosition;

    public Camera3D(float aspectRatio, float fieldOfView = MathHelper.PiOver4, float nearPlane = 0.1f, float farPlane = 10000f)
    {
        _targetPosition = Vector3.Zero;
        _distance = 10f;
        _rotationX = -MathHelper.PiOver4; // Start looking down at 45 degrees
        _rotationY = 0f;

        // Set up projection matrix
        _projection = Matrix.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, nearPlane, farPlane);

        // Initialize view matrix
        UpdateViewMatrix();
    }

    /// <summary>
    /// Updates the camera to view a specific target point from a given distance and rotation.
    /// </summary>
    /// <param name="targetPosition">The point in world space the camera should look at.</param>
    /// <param name="rotationX">Rotation around X-axis (pitch) in radians. Negative values look down.</param>
    /// <param name="rotationY">Rotation around Y-axis (yaw) in radians. Controls left/right orbit.</param>
    /// <param name="distance">Distance from the target point.</param>
    public void Update(Vector3 targetPosition, float rotationX, float rotationY, float distance)
    {
        _targetPosition = targetPosition;
        _rotationX = rotationX;
        _rotationY = rotationY;
        _distance = MathHelper.Max(distance, 0.1f); // Ensure distance is positive

        UpdateViewMatrix();
    }

    /// <summary>
    /// Recalculates the camera position and view matrix based on current parameters.
    /// </summary>
    private void UpdateViewMatrix()
    {
        // Calculate camera position using spherical coordinates around the target
        // Position = Target + Distance * (rotated direction)
        float cosX = (float)Math.Cos(_rotationX);
        float sinX = (float)Math.Sin(_rotationX);
        float cosY = (float)Math.Cos(_rotationY);
        float sinY = (float)Math.Sin(_rotationY);

        // Spherical coordinates:
        // X: sin(rotY) * cos(rotX) * distance
        // Y: sin(rotX) * distance
        // Z: cos(rotY) * cos(rotX) * distance
        _position = new Vector3(
            _targetPosition.X + sinY * cosX * _distance,
            _targetPosition.Y + sinX * _distance,
            _targetPosition.Z + cosY * cosX * _distance
        );

        // Create view matrix looking from position toward target
        _view = Matrix.CreateLookAt(_position, _targetPosition, Vector3.Up);
    }
}
