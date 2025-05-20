using UnityEngine;

public class FlagTrigger : MonoBehaviour
{
    public GameManager gm;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            gm.OnPlayerWin();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            gm.OnOhWait();
        }
    }
}