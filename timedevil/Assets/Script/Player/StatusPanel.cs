using TMPro;
using UnityEngine;

public class StatusPanel : MonoBehaviour
{
    private const string HP_FORMAT = "\uCCB4\uB825: {0}/{1}";
    private const string DEFENSE_FORMAT = "\uBC29\uC5B4\uB825: {0}";
    private const string ATTACK_FORMAT = "\uACF5\uACA9\uB825: {0}";
    private const string SPEED_FORMAT = "\uC18D\uB3C4: {0}";

    private TextMeshProUGUI hpLabel;
    private TextMeshProUGUI defenseLabel;
    private TextMeshProUGUI attackLabel;
    private TextMeshProUGUI speedLabel;

    void Awake()
    {
        CacheLabels();
    }

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        CacheLabels();

        if (PlayerDataRuntime.Instance == null || PlayerDataRuntime.Instance.Data == null)
            return;

        if (hpLabel == null || defenseLabel == null || attackLabel == null || speedLabel == null)
            return;

        PlayerData player = PlayerDataRuntime.Instance.Data;
        hpLabel.text = string.Format(HP_FORMAT, player.currentHP, player.maxHP);
        defenseLabel.text = string.Format(DEFENSE_FORMAT, player.defense);
        attackLabel.text = string.Format(ATTACK_FORMAT, player.attack);
        speedLabel.text = string.Format(SPEED_FORMAT, player.speed);
    }

    private void CacheLabels()
    {
        if (hpLabel == null)
            hpLabel = FindLabel("HPLabel");

        if (defenseLabel == null)
            defenseLabel = FindLabel("DefenseLabel");

        if (attackLabel == null)
            attackLabel = FindLabel("AttackLabel");

        if (speedLabel == null)
            speedLabel = FindLabel("SpeedLabel");
    }

    private TextMeshProUGUI FindLabel(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
    }
}
