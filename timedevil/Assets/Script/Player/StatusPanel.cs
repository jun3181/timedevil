using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class StatusPanel : MonoBehaviour, IDragHandler
{
    private const string HP_FORMAT = "체력: {0}/{1}";
    private const string DEFENSE_FORMAT = "방어력: {0}";
    private const string ATTACK_FORMAT = "공격력: {0}";
    private const string SPEED_FORMAT = "속도: {0}";

    private RectTransform screenTransform;
    private RectTransform panelTransform;

    private TextMeshProUGUI hpLabel;
    private TextMeshProUGUI defenseLabel;
    private TextMeshProUGUI attackLabel;
    private TextMeshProUGUI speedLabel;

    void Awake() {
        screenTransform = (RectTransform)transform.parent;
        panelTransform = (RectTransform)transform;

        hpLabel = transform.Find("HPLabel").GetComponent<TextMeshProUGUI>();
        defenseLabel = transform.Find("DefenseLabel").GetComponent<TextMeshProUGUI>();
        attackLabel = transform.Find("AttackLabel").GetComponent<TextMeshProUGUI>();
        speedLabel = transform.Find("SpeedLabel").GetComponent<TextMeshProUGUI>();
    }

    void OnEnable() {
        Debug.Log("b");
        if(PlayerDataRuntime.Instance == null) return;
        PlayerData player = PlayerDataRuntime.Instance.Data;

        hpLabel.text = string.Format(HP_FORMAT, player.currentHP, player.maxHP);
        defenseLabel.text = string.Format(DEFENSE_FORMAT, player.defense);
        attackLabel.text = string.Format(ATTACK_FORMAT, player.attack);
        speedLabel.text = string.Format(SPEED_FORMAT, player.speed);
    }

    public void OnDrag(PointerEventData eventData) {
        panelTransform.position += (Vector3)eventData.delta;

        Vector2 offsetMin = panelTransform.offsetMin;
        Vector2 offsetMax = panelTransform.offsetMax;
        if(offsetMin.x < 0) {
            offsetMax.x += -offsetMin.x;
            offsetMin.x = 0;
        } else if(offsetMax.x > 0) {
            offsetMin.x += -offsetMax.x;
            offsetMax.x = 0;
        }

        if(offsetMin.y < 0) {
            offsetMax.y += -offsetMin.y;
            offsetMin.y = 0;
        } else if(offsetMax.y > 0) {
            offsetMin.y += -offsetMax.y;
            offsetMax.y = 0;
        }

        panelTransform.offsetMin = offsetMin;
        panelTransform.offsetMax = offsetMax;
    }
}
