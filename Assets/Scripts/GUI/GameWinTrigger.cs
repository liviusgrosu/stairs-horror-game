using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameWinTrigger : MonoBehaviour
{
    [SerializeField] private Image blackScreen;
    [SerializeField] private TextMeshProUGUI winText1, winText2, winText3;
    [SerializeField] private float fadeTime = 2f;

    private bool _triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (!_triggered && other.CompareTag("Player"))
        {
            _triggered = true;
            StartCoroutine(StartWinScreen());
        }
    }

    private IEnumerator StartWinScreen()
    {
        // The win screen is disabled by default so its full-screen panel doesn't
        // overlap and block other UI (e.g. the settings menu). Enable it now that
        // the player has reached the win condition. blackScreen ("Background") is a
        // direct child of the "Game Win Screen" root, so its parent is that root.
        blackScreen.transform.parent.gameObject.SetActive(true);

        yield return FadeGraphic(blackScreen, 0f, 1f, fadeTime);

        GameManager.Instance.WinGame();

        yield return new WaitForSeconds(2f);
        yield return FadeGraphic(winText1, 0f, 1f, fadeTime / 2f);

        yield return new WaitForSeconds(2f);
        yield return FadeGraphic(winText2, 0f, 1f, fadeTime / 2f);

        yield return new WaitForSeconds(2f);
        yield return FadeGraphic(winText3, 0f, 1f, fadeTime / 2f);

        yield return new WaitForSeconds(4f);
        yield return FadeGraphics(new Graphic[] { winText1, winText2, winText3 }, 1f, 0f, fadeTime / 2f);
    }

    private IEnumerator FadeGraphic(Graphic graphic, float fromAlpha, float toAlpha, float duration)
    {
        var color = graphic.color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            graphic.color = color;
            yield return null;
        }
        color.a = toAlpha;
        graphic.color = color;
    }

    private IEnumerator FadeGraphics(Graphic[] graphics, float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            foreach (var graphic in graphics)
            {
                var color = graphic.color;
                color.a = alpha;
                graphic.color = color;
            }
            yield return null;
        }
        foreach (var graphic in graphics)
        {
            var color = graphic.color;
            color.a = toAlpha;
            graphic.color = color;
        }
    }
}
