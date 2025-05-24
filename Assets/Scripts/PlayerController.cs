using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) Debug.LogError("PlayerController: 找不到 Rigidbody2D 组件!");

        // 物理初始化
        // rb.gravityScale = 0f; // 根据你的游戏需要设置重力
        // rb.freezeRotation = true; // 冻结旋转
    }

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

    IEnumerator PhysicsFlipRoll(Vector2 direction, float duration)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        // 构造旋转中心
        Vector3 pivotOffset = new Vector3(direction.x, direction.y) * 0.5f + new Vector3(0, -0.5f, 0);
        Vector3 pivot = transform.position + pivotOffset;

        float elapsed = 0f;
        float totalAngle = -90f; // 顺时针
        float angleRotated = 0f;

        while (elapsed < duration)
        {
            float deltaTime = Time.deltaTime;
            float angleStep = totalAngle / duration * deltaTime;
            angleRotated += angleStep;

            // 手动旋转并计算新位置
            transform.RotateAround(pivot, Vector3.forward, angleStep);

            // 将旋转后的坐标更新到刚体上
            //rb.MovePosition(transform.position);
            rb.MoveRotation(transform.rotation.eulerAngles.z);

            elapsed += deltaTime;
            yield return null;
        }

        // 修正为整数格
        //rb.MovePosition(RoundPosition(startPos + new Vector3(direction.x, 0, 0)));
        //rb.MoveRotation(startRot.eulerAngles.z + totalAngle);
    }

    // 具体的指令执行逻辑协程 (保持不变)
    IEnumerator RunOption(string cmd)
    {
        if (cmd == "Right")
        {
            yield return PhysicsFlipRoll(Vector2.right, 0.4f);
            yield return new WaitForSeconds(0.3f);
        }
        else if (cmd == "Move_Right")
        {
            Vector3 startPos = transform.position;
            rb.MovePosition(startPos + new Vector3(Vector2.right.x, 0, 0));
            yield return new WaitForSeconds(0.5f);
        }
        else if (cmd == "Jump")
        {
            Debug.Log("执行步进跳跃指令");
            float jumpForce = 6f;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            yield return new WaitForSeconds(0.7f);
        }
        else if (cmd == "Observe")
        {
            Debug.Log("执行等待指令");
            yield return new WaitForSeconds(1f);
        }
        else if (cmd == "Pass")
        {
            yield return new WaitForSeconds(0.4f);
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
        rb.simulated = false;

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        transform.position = pos;
        transform.rotation = rot;
        transform.localScale = Vector3.one;

        rb.MovePosition(pos);
        rb.MoveRotation(rot.eulerAngles.z);

        // 重新启用物理模拟
        rb.simulated = true;
    }
}