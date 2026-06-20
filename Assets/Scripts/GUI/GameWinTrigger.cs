using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameWinTrigger : MonoBehaviour
{
    [SerializeField] private Image blackScreen;
    [SerializeField] private TextMeshProUGUI winText1, winText2, winText3;
    [Tooltip("Quit button revealed after the final line. Needs a CanvasGroup so it can fade in.")]
    [SerializeField] private CanvasGroup quitButton;
    [SerializeField] private float fadeTime = 2f;

    private bool _triggered;
    private Coroutine _sequence;
    private bool _skippable;

    private void OnTriggerEnter(Collider other)
    {
        if (!_triggered && other.CompareTag("Player"))
        {
            _triggered = true;
            _sequence = StartCoroutine(StartWinScreen());
        }
    }

    private void Update()
    {
        if (_skippable && Input.GetKeyDown(KeyCode.Escape))
        {
            SkipToReveal();
        }
    }

    private IEnumerator StartWinScreen()
    {
        blackScreen.transform.parent.gameObject.SetActive(true);

        if (GameManager.Instance) GameManager.Instance.WinGame();

        _skippable = true;

        yield return FadeGraphic(blackScreen, 0f, 1f, fadeTime);

        yield return new WaitForSeconds(2f);
        yield return FadeGraphic(winText1, 0f, 1f, fadeTime / 2f);

        yield return new WaitForSeconds(2f);
        yield return FadeGraphic(winText2, 0f, 1f, fadeTime / 2f);

        yield return new WaitForSeconds(2f);
        yield return FadeGraphic(winText3, 0f, 1f, fadeTime / 2f);

        yield return new WaitForSeconds(2f);
        yield return FadeCanvasGroup(quitButton, 0f, 1f, fadeTime / 2f);
        SetQuitButtonShown();

        _skippable = false;
        _sequence = null;

        yield return new WaitForSeconds(4f);
        yield return FadeGraphics(new Graphic[] { winText1, winText2, winText3 }, 1f, 0f, fadeTime / 2f);
    }

    private void SkipToReveal()
    {
        _skippable = false;

        if (_sequence != null)
        {
            StopCoroutine(_sequence);
            _sequence = null;
        }

        if (GameManager.Instance && !GameManager.Instance.HasWon)
        {
            GameManager.Instance.WinGame();
        }

        blackScreen.transform.parent.gameObject.SetActive(true);
        SetGraphicAlpha(blackScreen, 1f);
        SetGraphicAlpha(winText1, 1f);
        SetGraphicAlpha(winText2, 1f);
        SetGraphicAlpha(winText3, 1f);
        SetQuitButtonShown();
    }

    private void SetQuitButtonShown()
    {
        if (!quitButton) return;
        quitButton.alpha = 1f;
        quitButton.interactable = true;
        quitButton.blocksRaycasts = true;
    }

    private static void SetGraphicAlpha(Graphic graphic, float alpha)
    {
        if (!graphic) return;
        var color = graphic.color;
        color.a = alpha;
        graphic.color = color;
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

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float fromAlpha, float toAlpha, float duration)
    {
        if (!group) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            yield return null;
        }
        group.alpha = toAlpha;
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
