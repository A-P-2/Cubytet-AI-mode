using UnityEngine;

public class GlassEffectManager : MonoBehaviour
{
    [SerializeField] private GameObject effectObject;
    [SerializeField] private bool showWhenOn = true;

    private void OnEnable()
    {
        if (DataManager.HasData) ShowObject(DataManager.GlassEffect);
    }

    static public void ShowAllObjects(bool effectOn)
    {
        GlassEffectManager[] glassEffectManagers = FindObjectsOfType<GlassEffectManager>();
        foreach (var manager in glassEffectManagers) manager.ShowObject(effectOn);
    }

    private void ShowObject(bool effectOn) => effectObject.SetActive(effectOn == showWhenOn);
}
