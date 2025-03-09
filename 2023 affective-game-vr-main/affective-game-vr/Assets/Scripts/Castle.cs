using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Castle : MonoBehaviour
{
	public static Castle Instance { get; set; }

	[SerializeField] private int m_hp = 10;
	[SerializeField] private TMP_Text m_castleHpText = null;
	[SerializeField] private Image m_castleHpImage = null;
	[SerializeField] private string[] m_hpNames = new string[] {
					"Your castle has fallen!",
					"The gate barelly holds!",
					"The gate is damaged!",
					"Your castle is under attack!",
					"Your castle is untouched."};

	private int m_maxHp = 10;

	public int DamageReceivedInRound { get; set; } = 0;

	private void Awake()
	{
		Instance = this;
	}

	private void Start()
	{
		m_maxHp = m_hp;

		SetUI(1f);
	}

	public void Damage(int damage)
	{
		m_hp -= damage;

		float hpPercent = (float)m_hp / (float)m_maxHp;

		SetUI(hpPercent);

		DamageReceivedInRound++;
	}

	private void SetUI(float hpPercent)
	{
		m_castleHpText.text = m_hpNames[Mathf.Clamp(Mathf.FloorToInt(hpPercent * (m_hpNames.Length - 1)), 0, m_hpNames.Length)];

		m_castleHpImage.fillAmount = hpPercent;
	}
}
