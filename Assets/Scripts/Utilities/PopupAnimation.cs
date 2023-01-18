using UnityEngine;
using DG.Tweening;
using UnityEngine.Rendering;
public class PopupAnimation: MonoBehaviour
{
    [SerializeField]
    public GameObject destroyTarget;

    public void Present(System.Action callback = null)
    {
        transform.localScale = Vector2.zero;
        UIManager.Instance.IncreaseRenderFrame();
        if(callback == null) transform.DOScale(1f, 0.1f).OnComplete(() => {
            UIManager.Instance.DecreaseRenderFrame();
        });
        else transform.DOScale(1f, 0.1f).OnComplete(() => {
            UIManager.Instance.DecreaseRenderFrame();
            callback();
        });
    }

    public void Dismiss(System.Action callback = null)
    {
        UIManager.Instance.IncreaseRenderFrame();
        transform.DOScale(0f, 0.1f).OnComplete(() => {
            UIManager.Instance.DecreaseRenderFrame();
            if(callback != null) callback();
            if(destroyTarget != null) Destroy(destroyTarget);
        });
    }

    public void Dismiss()
    {
        UIManager.Instance.IncreaseRenderFrame();
        transform.DOScale(0f, 0.1f).OnComplete(() => {
            UIManager.Instance.DecreaseRenderFrame();
            if(destroyTarget != null) Destroy(destroyTarget);
        });
    }
}