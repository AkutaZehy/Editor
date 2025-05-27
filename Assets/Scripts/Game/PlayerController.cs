using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Dictionary<string, Func<IEnumerator>> _commandActions;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) Debug.LogError("PlayerController: 找不到 Rigidbody2D 组件!");

        // 物理初始化
        // rb.gravityScale = 0f; // 根据你的游戏需要设置重力
        // rb.freezeRotation = true; // 冻结旋转

        // Dict init
        _commandActions = new Dictionary<string, Func<IEnumerator>>
        {
            { "Right", () => RunRightOption() },
            { "Move_Right", () => RunMoveRightOption() },
            { "Jump", () => RunJumpOption() },
            { "Observe", () => RunWaitOption(1.0f) },
            { "Pass", () => RunWaitOption(0.4f) },
            { "Empty", () => RunWaitOption(0.25f) }
        };
    }

    // option list
    IEnumerator PhysicsFlipRoll(Vector2 direction, float duration)
    {
        // 构造旋转中心
        Vector3 pivotOffset = new Vector3(direction.x, direction.y) * 0.5f + new Vector3(0, -0.5f, 0);
        Vector3 pivot = transform.position + pivotOffset;

        float elapsed = 0f;
        float totalAngle = -90f; // 顺时针
        float angleRotated = 0f;

        while (elapsed < duration)
        {
            float deltaTime = Time.fixedDeltaTime;
            float angleStep = totalAngle / duration * deltaTime;
            angleRotated += angleStep;

            transform.RotateAround(pivot, Vector3.forward, angleStep);

            rb.MoveRotation(transform.rotation.eulerAngles.z);

            elapsed += deltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator WaitForFixedFrames(float durationInSeconds)
    {
        if (durationInSeconds <= 0f) yield break;

        int framesToWait = Mathf.Max(1, Mathf.CeilToInt(durationInSeconds / Time.fixedDeltaTime));

        for (int i = 0; i < framesToWait; i++) yield return new WaitForFixedUpdate();
    }
    private IEnumerator RunRightOption()
    {
        yield return PhysicsFlipRoll(Vector2.right, 0.4f);
        yield return StartCoroutine(WaitForFixedFrames(0.35f));
    }

    private IEnumerator RunMoveRightOption()
    {
        Vector3 startPos = transform.position;
        rb.MovePosition(startPos + new Vector3(Vector2.right.x, 0, 0));
        yield return StartCoroutine(WaitForFixedFrames(0.5f));
    }

    private IEnumerator RunJumpOption()
    {
        float jumpForce = 6f;
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        yield return StartCoroutine(WaitForFixedFrames(0.7f));
    }

    private IEnumerator RunWaitOption(float time)
    {
        yield return StartCoroutine(WaitForFixedFrames(time));
    }

    // main logic

    public void StopAllPlayerCoroutines()
    {
        StopAllCoroutines();
        Debug.Log("PlayerController: 已停止所有玩家动作协程.");
    }

    public Coroutine ExecuteSingleOption(string option)
    {
        StopAllCoroutines();
        return StartCoroutine(RunOption(option));
    }

    IEnumerator RunOption(string cmd)
    {
        Debug.Log("执行指令: " + cmd);

        if (_commandActions.TryGetValue(cmd, out Func<IEnumerator> actionFunc))
        {
            yield return actionFunc.Invoke();
        }
        else
        {
            Debug.LogWarning("未知的指令: " + cmd);
            yield return null;
        }

        Debug.Log("指令执行完毕: " + cmd);
    }

    public void ResetTo(Vector3 pos, Quaternion rot)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        StopAllCoroutines();

        // 暂停物理
        rb.bodyType = RigidbodyType2D.Static;

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        transform.position = pos;
        transform.rotation = rot;
        // transform.localScale = Vector3.one;

        // rb.MovePosition(pos);
        // rb.MoveRotation(rot.eulerAngles.z);

        rb.bodyType = RigidbodyType2D.Dynamic;
    }
}