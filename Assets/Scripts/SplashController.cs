using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RescueMateSplashController : MonoBehaviour
{
    [SerializeField] private float duration = 2.3f;
    [SerializeField] private Image loadingFill;
    [SerializeField] private Text loadingText;
    [SerializeField] private RectTransform heartIcon;

    private IEnumerator Start()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (loadingFill != null)
                loadingFill.fillAmount = Mathf.SmoothStep(0f, 1f, t);

            if (loadingText != null)
                loadingText.text = "Loading " + Mathf.RoundToInt(t * 100f) + "%";

            if (heartIcon != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.06f;
                heartIcon.localScale = Vector3.one * pulse;
            }

            yield return null;
        }

        SceneManager.LoadScene("MainMenu");
    }
}