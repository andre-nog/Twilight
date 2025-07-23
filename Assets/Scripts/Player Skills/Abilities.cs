using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class Abilities : MonoBehaviour
{
    [Header("Ability Q (Skillshot)")]
    public Key abilityKey = Key.Q;
    public Canvas abilityCanvas;
    public Image abilitySkillshot;
    public float skillRange = 8f;
    public float spellRadius = 0.5f;

    [Header("SmartCast UI")]
    public TextMeshProUGUI smartCastTextUI;

    private bool isPreparingFireball = false;
    private bool smartCastEnabled = false;

    private Vector3 targetPoint;
    private RaycastHit hit;
    private Ray ray;

    private PlayerMagicSystem magicSystem;

    void Start()
    {
        abilitySkillshot.enabled = false;
        abilityCanvas.enabled = false;

        SetSkillshotLength(skillRange, spellRadius);

        magicSystem = GetComponent<PlayerMagicSystem>();

        if (smartCastTextUI != null)
            smartCastTextUI.gameObject.SetActive(false);
    }

    void Update()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        ray = Camera.main.ScreenPointToRay(mouseScreenPos);

        ToggleSmartCast();
        HandleInput();
        UpdateCanvasRotation();
        CheckMouseClickToCast();
    }

    private void ToggleSmartCast()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            smartCastEnabled = !smartCastEnabled;
            string status = smartCastEnabled ? "Smart Cast Ativado" : "Smart Cast Desativado";
            Debug.Log($"SmartCast: {status}");

            if (smartCastTextUI != null)
                StartCoroutine(ShowSmartCastText(status));
        }
    }

    private IEnumerator ShowSmartCastText(string message)
    {
        smartCastTextUI.text = message;
        smartCastTextUI.gameObject.SetActive(true);

        yield return new WaitForSeconds(2f);

        smartCastTextUI.gameObject.SetActive(false);
    }
    private void ShowMessage(string message)
    {
        if (smartCastTextUI != null)
            StartCoroutine(ShowSmartCastText(message));
    }

    private void HandleInput()
    {
        if (Keyboard.current[abilityKey].wasPressedThisFrame)
        {
            if (smartCastEnabled)
            {
                Vector3 aim = GetClampedTargetPoint(magicSystem.MouseWorldPoint, skillRange + spellRadius);
                magicSystem.TryCastFireballAt(aim);
            }
            else if (!isPreparingFireball)
            {
                if (Time.time >= magicSystem.FireballReadyTime)
                {
                    Cursor.visible = true;
                    abilitySkillshot.enabled = true;
                    abilityCanvas.enabled = true;
                    isPreparingFireball = true;
                }
                else
                {
                    ShowMessage("Skill em cooldown");
                }
            }
        }
    }

    private void CheckMouseClickToCast()
    {
        if (!isPreparingFireball) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            magicSystem.TryCastFireballAt(targetPoint);

            abilitySkillshot.enabled = false;
            abilityCanvas.enabled = false;
            isPreparingFireball = false;
        }
    }

    private void UpdateCanvasRotation()
    {
        if (!abilitySkillshot.enabled) return;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            targetPoint = GetClampedTargetPoint(hit.point, skillRange + spellRadius);
        }

        Quaternion targetRotation = Quaternion.LookRotation(targetPoint - transform.position);
        targetRotation.eulerAngles = new Vector3(0, targetRotation.eulerAngles.y, targetRotation.eulerAngles.z);

        abilityCanvas.transform.rotation = Quaternion.Lerp(targetRotation, abilityCanvas.transform.rotation, 0);
    }

    private Vector3 GetClampedTargetPoint(Vector3 hitPoint, float maxDistance)
    {
        Vector3 direction = hitPoint - transform.position;
        float distance = Mathf.Min(direction.magnitude, maxDistance);
        return transform.position + direction.normalized * distance;
    }

    private void SetSkillshotLength(float range, float radius)
    {
        if (abilitySkillshot != null && abilitySkillshot.rectTransform != null)
        {
            float heightPerRangeUnit = 7.125f;
            float zOffsetPerRangeUnit = 0.175f;
            float widthPerRadiusUnit = 13f;

            float visualHeight = range * heightPerRangeUnit;
            float visualOffset = range * zOffsetPerRangeUnit;
            float visualWidth = radius * widthPerRadiusUnit;

            var size = abilitySkillshot.rectTransform.sizeDelta;
            size.y = visualHeight;
            size.x = visualWidth;
            abilitySkillshot.rectTransform.sizeDelta = size;

            Vector3 pos = abilitySkillshot.rectTransform.localPosition;
            pos.z = visualOffset;
            abilitySkillshot.rectTransform.localPosition = pos;
        }
    }
}
