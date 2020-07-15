using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class PlayerInput : MonoBehaviour
{
    Text messageText, timerText;

    Rigidbody2D rb;
    CircleCollider2D cc;

    Vector2 kickVector = Vector2.up;

    GameObject[] goals;

    bool hasWon = false;
    int levelIndex = 0;
    float levelTimer = 0.0f;

    [SerializeField]
    CircleCollider2D kickCollider = null;
    [SerializeField]
    float kickDist = 0.5f, kickRadius = 0.4f, kickVelocity = 20.4f;
    [SerializeField]
    int kickDelayFrames = 17, kickDurationFrames = 7;
    [SerializeField]
    GameObject[] levelStartLocations = null, cameraLocations = null;
    [SerializeField]
    [TextArea(1,6)]
    string[] levelMessages = null;
    [SerializeField]
    AudioClip musicClip, missClip, hitClip, winClip, loseClip;

    void Start()
    {
        Cursor.visible = false;

        messageText = GameObject.Find("Message").GetComponent<Text>();
        timerText = GameObject.Find("Timer").GetComponent<Text>();

        rb = GetComponent<Rigidbody2D>();
        cc = GetComponent<CircleCollider2D>();

        if (kickCollider == null)
        {
            Debug.LogError("No kick collider provided.");
        }

        kickCollider.transform.localScale = 2.0f * kickRadius * Vector3.one;
        kickCollider.transform.localPosition = -kickDist * kickVector;

        goals = GameObject.FindGameObjectsWithTag("Goal");

        RestartLevel();
        StartCoroutine(Kicking());

        if (levelMessages != null)
        {
            DisplayMessage(levelMessages[levelIndex]);
        }
    }

    bool givenTime = false;
    void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            if (levelIndex != 9)
            {
                levelIndex = 8;
                GoToNextLevel();

                if (levelMessages != null)
                {
                    DisplayMessage(levelMessages[levelIndex]);
                }
            }
            else
            {
                Application.Quit();
            }
        }

        if (!givenTime && hasWon && rb.velocity == Vector2.zero)
        {
            StartCoroutine(CheckWin());
            givenTime = true;
        }

        if (Input.GetButtonDown("Reload"))
        {
            if (messageText.text == "")
            {
                DisplayMessage(levelMessages[levelIndex]);
            }

            RestartLevel();
            return;
        }

        if (Input.GetButtonUp("Submit"))
        {
            GoToNextLevel();

            if (levelMessages != null)
            {
                DisplayMessage(levelMessages[levelIndex]);
            }
        }

        if (!hasWon)
        {
            levelTimer += Time.deltaTime;
        }

        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        if (x != 0.0f)
        {
            x = Mathf.Sign(x);
        }

        if (y != 0.0f)
        {
            y = Mathf.Sign(y);
        }

        float kickAngle = x != 0.0f || y != 0.0f ? Mathf.Atan2(y, x) : 0.5f * Mathf.PI;
        CalculateKickVector(kickAngle);
        kickCollider.transform.localPosition = -kickDist * kickVector;

        LayerMask mask = LayerMask.GetMask("Hazards");
        if (cc.IsTouchingLayers(mask))
        {
            RestartLevel();
            return;
        }

        mask = LayerMask.GetMask("Goals");
        if (cc.IsTouchingLayers(mask) || kickCollider.IsTouchingLayers(mask))
        {
            WinLevel();
            return;
        }
    }

    IEnumerator CheckWin()
    {
        yield return new WaitForSeconds(0.5f);

        if (hasWon)
        {
            DisplayTimer();
        }
    }

    void WinLevel()
    {
        hasWon = true;
        if (goals.Length > 0)
        {
            foreach (GameObject goal in goals)
            {
                goal.SetActive(false);
            }
        }
    }

    void DisplayMessage(string text)
    {
        if (Typing != null)
        {
            StopCoroutine(Typing);
        }

        Typing = StartCoroutine(TypeText(messageText, text));
    }

    void DisplayTimer()
    {
        if (Typing != null)
        {
            StopCoroutine(Typing);
        }

        Typing = StartCoroutine(TypeText(timerText, (Mathf.Round(1000.0f * levelTimer) / 1000.0f).ToString()));
    }

    Coroutine Typing;
    IEnumerator TypeText(Text textObject, string text)
    {
        messageText.text = "";
        timerText.text = "";

        int i = 0;
        while (textObject.text != text)
        {
            textObject.text += text[i++];
            yield return null;
        }

        Typing = null;
    }

    void GoToNextLevel()
    {
        rb.velocity = Vector2.zero;
        kickVector = Vector2.up;
        hasWon = false;
        givenTime = false;
        levelIndex++;
        levelTimer = 0.0f;

        if (goals.Length > 0)
        {
            foreach (GameObject goal in goals)
            {
                goal.SetActive(true);
            }
        }

        if (levelStartLocations != null)
        {
            if (levelIndex == levelStartLocations.Length)
            {
                levelIndex = 0;
            }

            transform.position = levelStartLocations[levelIndex].transform.position;

            if (cameraLocations != null)
            {
                Camera.main.transform.position = cameraLocations[levelIndex].transform.position;
            }
            else
            {
                Camera.main.transform.position = transform.position + 10.0f * Vector3.back;
            }
        }
        else
        {
            transform.position = Vector2.zero;
            Camera.main.transform.position = 10.0f * Vector3.back;
        }
    }

    void RestartLevel()
    {
        levelIndex--;
        GoToNextLevel();
    }

    void CalculateKickVector(float angle)
    {
        kickVector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    IEnumerator Kicking()
    {
        int kickTimer = 0;
        while (true)
        {
            if (!hasWon)
            {
                if (kickTimer == kickDelayFrames)
                {
                    kickTimer = 0;
                    StartCoroutine(CheckingKick());
                }
            }
            else
            {
                kickTimer = 0;
            }

            yield return new WaitForFixedUpdate();
            kickTimer++;
        }
    }

    void AddKickVelocity()
    {
        Vector2 newVel = kickVelocity * kickVector;
        if (Mathf.Abs(kickVector.x) < 0.001f || Mathf.Sign(kickVector.x) == Mathf.Sign(rb.velocity.x))
        {
            newVel.x += rb.velocity.x;
        }

        if (Mathf.Abs(kickVector.y) < 0.001f || Mathf.Sign(kickVector.y) == Mathf.Sign(rb.velocity.y))
        {
            newVel.y += rb.velocity.y;
        }

        rb.velocity = newVel;
    }

    IEnumerator CheckingKick()
    {
        kickCollider.GetComponent<SpriteRenderer>().color = Color.red;

        bool done = false;
        int t = 0;
        while (t < kickDurationFrames)
        {
            if (!done)
            {
                LayerMask mask = LayerMask.GetMask("Boosts");
                if (cc.IsTouchingLayers(mask) || kickCollider.IsTouchingLayers(mask))
                {
                    AddKickVelocity();
                    done = true;
                }
                else
                {
                    mask = LayerMask.GetMask("Boosts", "Surfaces");
                    if (kickCollider.IsTouchingLayers(mask))
                    {
                        AddKickVelocity();
                        done = true;
                    }
                }
            }

            yield return new WaitForFixedUpdate();
            t++;
        }

        kickCollider.GetComponent<SpriteRenderer>().color = Color.black;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Goal"))
        {
            WinLevel();
        }
    }
}