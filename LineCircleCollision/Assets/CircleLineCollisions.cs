/// <summary>
/// 2D Line-Circle Collision using information from:
/// http://doswa.com/2009/07/13/circle-segment-intersectioncollision.html
/// http://seb.ly/2010/01/predicting-circle-line-collisions/
/// 
/// Collision occurs only with 2D Colliders.
/// Attach script to any GameObject in scene.
/// 
/// Implemented in Unity3D v5.5.2f1 by Jared Hemmings, July 2017.
/// 
/// jaredhemmings.wordpress.com
/// </summary>

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleLineCollisions : MonoBehaviour 
{
	public Transform circle;
	[Range(1,40)]
	public float circleSpeed = 10;

	private Vector2 circleDir;
	private bool moveCircle = false;
	private float circleRadius = 0.2f;

	// OnDrawGizmos variables
	private LinePoints lineGizmo;
	private Vector2 dest;


	void Start()
	{
		// Set the fixed frame update time manually to increase collision check frequency
		// and smooth out movement.
		Time.fixedDeltaTime = 0.005f;

		// If no circle has been added in the Inspector, create one.
		if (circle == null)
		{
			circle = GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<Transform>();
			circle.localScale =  new Vector3(circleRadius * 2,circleRadius * 2,0.1f);
			circle.position = new Vector2(0,0);

		}

		// Set the circleRadius based on the the linked GameObject.
		if (circle != null)
		{
			circleRadius = circle.localScale.x / 2;
		}
	}

	void FixedUpdate()
	{
		// Used to initially start the circle & manually change it's circleDirection.
		if (Input.GetMouseButtonDown(0))
		{
			circleDir = (Camera.main.ScreenToWorldPoint((Vector2)Input.mousePosition) - circle.transform.position).normalized;
			circle.Translate(circleDir.normalized * circleSpeed * Time.deltaTime);
			moveCircle = true;
		}


		if (moveCircle)
		{
			Vector2 circlePos = (Vector2)circle.position;
			circle.position = Movecircle(circlePos, circlePos + (circleDir.normalized * circleSpeed * Time.fixedDeltaTime));
		}
	}

	Vector2 Movecircle(Vector2 circlePos,Vector2 nextCirclePos)
	{
		// Look for the next collision from the current position and direction.
		RaycastHit2D hit = Physics2D.CircleCast(circlePos, circleRadius, circleDir, 100f);
		LinePoints newLine = GetLine(hit.collider.bounds, hit.centroid);

		// Only used for OnDrawGizmos.
		lineGizmo = newLine;
		dest = hit.centroid;

		// Get the closest collision point on the current line.
		Vector2 collisionPoint = ClosestPointOnSegment(newLine.p1, newLine.p2, hit.centroid);

		// Find distances of current and next circle position.
		Vector2 currentCircleDirection = new Vector2(circlePos.x-collisionPoint.x,circlePos.y-collisionPoint.y);
		float currentDist = Vector2.Dot(currentCircleDirection,hit.normal);
		Vector2 nextCircleDirection = new Vector2(nextCirclePos.x-collisionPoint.x,nextCirclePos.y-collisionPoint.y);
		float nextDist = Vector2.Dot(nextCircleDirection,hit.normal);

		// Find the time until the circle collides with the line.
		float timeToCollision = (circleRadius - currentDist) / (nextDist - currentDist);

		// Check timeToCollision to see if a collision will happen in the next frame.
		if (timeToCollision >= 0 && timeToCollision <= 1)
		{
			// If the timeToCollision is less than 1 (1 frame), move the circle to the distance before collision.
			Vector2 betweenFrameDirection = new Vector2(nextCirclePos.x - circlePos.x, nextCirclePos.y - circlePos.y);
			nextCirclePos = circlePos + (betweenFrameDirection * timeToCollision);
			circleDir = Vector2.Reflect(circleDir,hit.normal);
		}

		// return the circle's next position.
		return nextCirclePos;
	}

	// Use projection to find where the circle collision point will be on the line.
	Vector2 ClosestPointOnSegment(Vector2 lineP1, Vector2 lineP2, Vector2 circleCenter)
	{
		
		Vector2 segmentVector = lineP2 - lineP1;
		Vector2 pointVector = circleCenter - lineP1;

		if (segmentVector.magnitude <= 0)
			Debug.LogError("Invalid segment length");

		Vector2 segmentVectorUnit = segmentVector / segmentVector.magnitude;
		float lineProjection = Vector2.Dot(pointVector,segmentVectorUnit);

		if (lineProjection <= 0)
			return lineP1;
		if (lineProjection >= segmentVector.magnitude)
			return lineP2;

		Vector2 projectionVector = segmentVectorUnit * lineProjection;
		Vector2 closestPointOnLineSegment = projectionVector + lineP1;
		return closestPointOnLineSegment;								
	}

	// Returns the relevant end points of the Collider2D.
	LinePoints GetLine(Bounds hitArea, Vector2 hitPos)
	{
		// Bottom of collider.
		if (hitPos.y < (hitArea.center.y - hitArea.extents.y) && hitPos.x < (hitArea.center.x + hitArea.extents.x + circleRadius) && hitPos.x > (hitArea.center.x - hitArea.extents.x - circleRadius))
		{
			return new LinePoints(
				new Vector2(hitArea.center.x - hitArea.extents.x, hitArea.center.y - hitArea.extents.y),
				new Vector2(hitArea.center.x + hitArea.extents.x, hitArea.center.y - hitArea.extents.y)
			);
		}

		// Top of collider.
		if (hitPos.y > (hitArea.center.y + hitArea.extents.y) && hitPos.x < (hitArea.center.x + hitArea.extents.x + circleRadius) && hitPos.x > (hitArea.center.x - hitArea.extents.x - circleRadius))
		{
			return new LinePoints(
				new Vector2(hitArea.center.x - hitArea.extents.x, hitArea.center.y + hitArea.extents.y),
				new Vector2(hitArea.center.x + hitArea.extents.x, hitArea.center.y + hitArea.extents.y)
			);
		}

		// Left of collider.
		if (hitPos.x < (hitArea.center.x - hitArea.extents.x) && hitPos.y < (hitArea.center.y + hitArea.extents.y + circleRadius) && hitPos.y > (hitArea.center.y - hitArea.extents.y - circleRadius))
		{
			return new LinePoints(
				new Vector2(hitArea.center.x - hitArea.extents.x, hitArea.center.y - hitArea.extents.y),
				new Vector2(hitArea.center.x - hitArea.extents.x, hitArea.center.y + hitArea.extents.y)
			);
		}

		// Right of collider.
		if (hitPos.x > (hitArea.center.x + hitArea.extents.x) && hitPos.y < (hitArea.center.y + hitArea.extents.y + circleRadius) && hitPos.y > (hitArea.center.y - hitArea.extents.y - circleRadius))
		{
			return new LinePoints(
				new Vector2(hitArea.center.x + hitArea.extents.x, hitArea.center.y - hitArea.extents.y),
				new Vector2(hitArea.center.x + hitArea.extents.x, hitArea.center.y + hitArea.extents.y)
			);
		}

		Debug.LogError("Unable to find line points.");
		return new LinePoints(Vector2.zero,Vector2.zero);
	}
		
	private struct LinePoints
	{
		public Vector2 p1 { get; }
		public Vector2 p2 { get; }

		public LinePoints(Vector2 p1,Vector2 p2)
		{
			this.p1 = p1;
			this.p2 = p2;
		}
	}

	void OnDrawGizmos()
	{
		// Draw current direction line.
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(dest, circleRadius);
		Gizmos.DrawLine(circle.position,dest);

		// Draw the collision line.
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireSphere(lineGizmo.p1, circleRadius);
		Gizmos.DrawWireSphere(lineGizmo.p2, circleRadius);
		Gizmos.DrawLine(lineGizmo.p1, lineGizmo.p2);
	}
}