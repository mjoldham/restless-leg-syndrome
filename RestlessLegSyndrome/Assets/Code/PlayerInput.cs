using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class PlayerInput : MonoBehaviour
{
    Rigidbody2D rb;
    CircleCollider2D cc;
    Transform kick;
    float kickAngle = -0.5f * Mathf.PI;

    [SerializeField]
    float kickDelay = 0.5f, kickDist = 0.5f, kickRadius = 0.45f, kickVelocity = 15.0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        kick = transform.Find("kick");
        InvokeRepeating("Kick", 0.0f, kickDelay);
    }

    void Update()
    {
        float x = 0.0f;
        float y = 0.0f;

        if (Input.GetKey(KeyCode.A))
        {
            x -= 1.0f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            x += 1.0f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            y -= 1.0f;
        }

        if (Input.GetKey(KeyCode.W))
        {
            y += 1.0f;
        }

        kickAngle = x != 0.0f || y != 0.0f ? Mathf.Atan2(y, x) : -0.5f * Mathf.PI;
    }

    void Kick()
    {
        Vector2 kickVector = new Vector2(Mathf.Cos(kickAngle), Mathf.Sin(kickAngle));
        kick.gameObject.SetActive(true);
        kick.localPosition = kickDist * kickVector;
        Invoke("HideKick", 0.2f);

        if (Physics2D.OverlapCircle(rb.position + kickDist * kickVector, kickRadius, LayerMask.GetMask("Surfaces", "NPC")) != null)
        {
            Vector2 newVel = -kickVelocity * kickVector;
            float parallel = Vector2.Dot(rb.velocity, -kickVector);
            if (parallel > 0.0f)
            {
                newVel += parallel * kickVector;
            }

            newVel += rb.velocity - parallel * kickVector;
            rb.velocity = newVel;
        }
    }

    void HideKick()
    {
        kick.gameObject.SetActive(false);
    }
}
