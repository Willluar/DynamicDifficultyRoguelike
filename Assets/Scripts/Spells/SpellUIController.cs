using UnityEngine;
using UnityEngine.UI;

public class SpellUIController : MonoBehaviour
{
    [Header("Spell Icons")]
    public Image iceIcon;
    public Image lightningIcon;
    public Image fireIcon;

    [Header("Colours")]
    public Color selectedColor = Color.white;
    public Color unselectedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Scale")]
    public Vector3 selectedScale = new Vector3(1.15f, 1.15f, 1f);
    public Vector3 unselectedScale = Vector3.one;

    public void SetSelectedSpell(SpellType spellType)
    {
        ResetIcons();

        switch (spellType)
        {
            case SpellType.IceBolt:
                HighlightIcon(iceIcon);
                break;

            case SpellType.LightningBolt:
                HighlightIcon(lightningIcon);
                break;

            case SpellType.Fireball:
                HighlightIcon(fireIcon);
                break;
        }
    }

    private void ResetIcons()
    {
        SetIconState(iceIcon, false);
        SetIconState(lightningIcon, false);
        SetIconState(fireIcon, false);
    }

    private void HighlightIcon(Image icon)
    {
        SetIconState(icon, true);
    }

    private void SetIconState(Image icon, bool selected)
    {
        if (icon == null) return;

        icon.color = selected ? selectedColor : unselectedColor;
        icon.transform.localScale = selected ? selectedScale : unselectedScale;
    }
}