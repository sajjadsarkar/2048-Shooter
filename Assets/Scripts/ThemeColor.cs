using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class ThemeColor : MonoBehaviour
{
    public ThemeRole role;

    private void OnEnable()
    {
        Theme2048.Changed += Apply;
        Apply();
    }

    private void OnDisable() => Theme2048.Changed -= Apply;

    private void Apply() => GetComponent<Graphic>().color = Theme2048.Get(role);
}
