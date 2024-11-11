using UnityEngine;
using TMPro;

public class Scorecount : MonoBehaviour
{
    [SerializeField] private TMP_Text score;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        score.text = TankAgent.Score.ToString();
        // score.text = AItank.Getscore().ToString();
    }
}
