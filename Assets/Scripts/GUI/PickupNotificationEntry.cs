using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PickupNotificationEntry : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float fadeDuration = 0.5f;

    private CanvasGroup _canvasGroup;
    private RectTransform _rt;
    private float _targetY;
    private float _slideSpeed;
    private PickupNotification _manager;

    public void Init(InventoryItem item, float slideSpeed, PickupNotification manager)
    {
        _manager = manager;
        _slideSpeed = slideSpeed;

        _rt = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (!_canvasGroup)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        _canvasGroup.alpha = 0f;
        _targetY = _rt.anchoredPosition.y;

        iconImage.sprite = item.Icon;
        nameText.text = item.Name;

        StartCoroutine(LifetimeRoutine());
    }

    public void PushUp(float amount)
    {
        _targetY += amount;
    }

    private void Update()
    {
        if (!_rt)
        {
            return;
        }

        var pos = _rt.anchoredPosition;
        pos.y = Mathf.Lerp(pos.y, _targetY, Time.deltaTime * _slideSpeed);
        _rt.anchoredPosition = pos;
    }

    private IEnumerator LifetimeRoutine()
    {
        var t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(displayDuration);

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }

        _manager.RemoveEntry(this);
        Destroy(gameObject);
    }
}
