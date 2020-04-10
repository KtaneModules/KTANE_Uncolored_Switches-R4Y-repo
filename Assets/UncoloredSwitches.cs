using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public class UncoloredSwitches : MonoBehaviour {
	static int moduleIDCounter = 1;
	int moduleID;
	int stage = 0;
	int boom = 0;
	int Logic_gate_index = 0;
	public Material[] LEDsColors;
	public GameObject[] Switches;
	public GameObject[] LEDs;
	public KMBombInfo Bomb;
	public KMBombModule Module;
	private Coroutine submitCoroutine;
	StringBuilder Switches_Current_State = new StringBuilder("00000");
	StringBuilder Switches_State = new StringBuilder("00000");
	StringBuilder Submission_State = new StringBuilder("00000");
	StringBuilder LEDsColorsString = new StringBuilder("KKKKKKKKKK");
	string Logic_gate = "";
	string[][] LOGIC_GATES = new string[9][];
	string[] gates = { "AND", "OR", "XOR", "NAND", "NOR", "XNOR", "IMPLIES", "IMPLIED BY" };
	string[] order = { "first", "second", "third" };
	string[] AND_States = {"11110","00010","00100","10010"};
	string[] OR_States = {"00011","10100","10110","10000"};
	string[] XOR_States = {"10101","01110","01001","01101"};
	string[] NAND_States = {"01100","10111","11111","11001"};
	string[] NOR_States = {"01111","01010","00001","11011"};
	string[] XNOR_States = {"00111","00000","11010","00110"};
	string[] IMPLIES_States = {"00101","11101","11100","01000"};
	string[] IMPLIED_BY_States = {"11000","10011","10001","01011"};
	string[] Original_Switches_States = { "00100", "01011", "01111", "10010", "10011", "10111", "11000", "11010", "11100", "11110" };
	//Debug.LogFormat("[Uncolored Switches #{0}]", moduleID);
	void Start () 
	{
		LOGIC_GATES[0] = AND_States;
		LOGIC_GATES[1] = OR_States;
		LOGIC_GATES[2] = XOR_States;
		LOGIC_GATES[3] = NAND_States;
		LOGIC_GATES[4] = NOR_States;
		LOGIC_GATES[5] = XNOR_States;
		LOGIC_GATES[6] = IMPLIES_States;
		LOGIC_GATES[7] = IMPLIED_BY_States;
		moduleID = moduleIDCounter++;
		int random_flip = 0;
		for(int i = 0; i < 5; i++)
		{
			random_flip = Random.Range(0, 2);
			if (random_flip == 1) { StartCoroutine(FlipSwitch(i));	Switches_State[i] = '1'; }
		}
		GenerateStage();
		for (int i = 0; i < 5; i++)
		{
			int j = i;
			Switches[j].GetComponent<KMSelectable>().OnInteract += delegate () 
			{
				if(stage != 4)
                {
					if (submitCoroutine != null)
						StopCoroutine(submitCoroutine);
					submitCoroutine = StartCoroutine(Submission());
				}
				StartCoroutine(FlipSwitch(j));
				GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Switches[j].transform); 
				Switches[j].GetComponent<KMSelectable>().AddInteractionPunch(.25f); 
				return false; 
			};
		}
	}
	void Update()
	{
		if (Bomb.GetStrikes() > boom && boom == 0 && LEDsColorsString.ToString().Contains("R"))
		{
			boom++;
			Debug.LogFormat("[Uncolored Switches #{0}] There is a new strike occured on this bomb", moduleID, Logic_gate);
			GenerateSubmission();
		}
	}
	void GenerateStage()
	{
		Debug.LogFormat("[Uncolored Switches #{0}] The current switches position is {1}", moduleID, NumbersToArrows(Switches_State));
		GenerateLEDS();
		Debug.LogFormat("[Uncolored Switches #{0}] The {1} stage's top LEDs colors in reading order are: {2}", moduleID, order[stage], LEDsColorsString.ToString(0, 5));
		Debug.LogFormat("[Uncolored Switches #{0}] The {1} stage's top LEDs colors in reading order are: {2}", moduleID, order[stage], LEDsColorsString.ToString(5, 5));
		DetectState();
		for(int i = 0; i < 5; i++)
        {
			Switches_Current_State[i] = Switches_State[i];
		}
		
		Debug.LogFormat("[Uncolored Switches #{0}] The logic gate is: {1}", moduleID, Logic_gate);
		GenerateSubmission();
	}
	void GenerateSubmission()
	{
		for (int i = 0; i < 5; i++)
		{
			switch (Logic_gate_index)
			{
				case 0:
					Submission_State[i] = BoolToNumber(RuleApplication(LEDsColorsString[i]) && RuleApplication(LEDsColorsString[i + 5]));
					break;
				case 1:
					Submission_State[i] = BoolToNumber(RuleApplication(LEDsColorsString[i]) || RuleApplication(LEDsColorsString[i + 5]));
					break;
				case 2:
					Submission_State[i] = BoolToNumber(RuleApplication(LEDsColorsString[i]) ^ RuleApplication(LEDsColorsString[i + 5]));
					break;
				case 3:
					Submission_State[i] = BoolToNumber(!(RuleApplication(LEDsColorsString[i]) && RuleApplication(LEDsColorsString[i + 5])));
					break;
				case 4:
					Submission_State[i] = BoolToNumber(!(RuleApplication(LEDsColorsString[i]) || RuleApplication(LEDsColorsString[i + 5])));
					break;
				case 5:
					Submission_State[i] = BoolToNumber(!(RuleApplication(LEDsColorsString[i]) ^ RuleApplication(LEDsColorsString[i + 5])));
					break;
				case 6:
					Submission_State[i] = BoolToNumber(!(RuleApplication(LEDsColorsString[i]) && !RuleApplication(LEDsColorsString[i + 5])));
					break;
				case 7:
					Submission_State[i] = BoolToNumber(!(!RuleApplication(LEDsColorsString[i]) &&  RuleApplication(LEDsColorsString[i + 5])));
					break;
			}
		}
		Debug.LogFormat("[Uncolored Switches #{0}] The submission is {1}", moduleID,NumbersToArrows(Submission_State));
	}
	IEnumerator Submission()
	{
		bool no = false;
		yield return new WaitForSeconds(2.0f);
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Switches[1].transform);
		for (int i = 0; i < 5; i++)
		{
			if (Submission_State[i] != Switches_State[i]) 
			{ 
				no = true;  
				Module.HandleStrike(); 
				Debug.LogFormat("[Uncolored Switches #{0}] Strike! You submitted {1}, but it was expected to be {2}", moduleID, NumbersToArrows(Switches_State), NumbersToArrows(Submission_State));
				break; 
			}
			if (i == 4) stage++;
		}
		if (!no && stage == 3)
		{
			Module.HandlePass();
			for(int i = 0; i < 10; i++) LEDs[i].GetComponent<MeshRenderer>().material = LEDsColors[7];
			stage++;
			Debug.LogFormat("[Uncolored Switches #{0}] Congratulations! You solved the module!", moduleID);
		}
		else if(!no) GenerateStage();
		if (no)
        {
			for (int i = 0; i < 5; i++)
			{
				if (NumberToBool(Switches_State[i]) != NumberToBool(Switches_Current_State[i])) { StartCoroutine(FlipSwitch(i)); }
			}
			for (int i = 0; i < 5; i++)
			{
				Switches_State[i] = Switches_Current_State[i];
			}
		}

	}

	char BoolToNumber(bool b)
	{
		if (b == true) return '1';
		else return '0';
	}
	bool NumberToBool(char num)
	{
		if (num == '1') return true;
		else return false;
	}
	
	bool RuleApplication(char color)
	{
		bool b = false;
		switch (color)
		{
			case 'R':
				b = RedRule();
				break;
			case 'G':
				b = GreenRule();
				break;
			case 'B':
				b = BlueRule();
				break;
			case 'T':
				b = TurquoiseRule();
				break;
			case 'O':
				b = OrangeRule();
				break;
			case 'P':
				b = PurpleRule();
				break;
			case 'W':
				b = WhiteRule();
				break;
			case 'K':
				b = BlackRule();
				break;
		}
		return b;
	}
	void DetectState()
	{
		for(int i = 0; i < 8; i++)
		{
			for(int j = 0; j < 4; j++)
			{
				if (Switches_State.ToString() == LOGIC_GATES[i][j])
				{
					Logic_gate = gates[i];
					Logic_gate_index = i;
					
				}
			}
		}
	}
	StringBuilder NumbersToArrows(StringBuilder numbers)
	{
		StringBuilder arrows= new StringBuilder("▲▲▲▲▲");
		for(int i = 0; i < 5; i++) {if (numbers[i] == '0') { arrows[i] = '▼'; }	}
		return arrows;
	}
	
	
	
	bool WhiteRule()
	{
		bool ball = false;
		if (IsUpMoreThanDown(Switches_State)) { ball = true; }
		return ball;
	}
	bool BlackRule()
	{
		bool ball = false;
		if (IsMostOneWhite(LEDsColorsString)) { ball = true; }
		return ball ;
	}
	bool TurquoiseRule()
	{
		bool ball = false;
		int RBcounter = 0;
		int OPcounter = 0;
		for (int i = 0; i < 10; i++)
		{
			if (LEDsColorsString[i] == 'R' || LEDsColorsString[i] == 'B') RBcounter++;
			if (LEDsColorsString[i] == 'O' || LEDsColorsString[i] == 'P') OPcounter++;
		}
		if (RBcounter > OPcounter) { ball = true; }
		return ball;
	}
	bool PurpleRule()
	{
		bool ball = false;
		if (Bomb.GetModuleNames().Any(x => x.ToLowerInvariant().Contains("forget") || x == "Souvenir" || x == "Turn The Key" || x.ToLowerInvariant().Contains("needy"))) { ball = true; }
		return ball;
	}
	bool OrangeRule()
	{
		bool ball = false;
		if (Bomb.GetBatteryCount() > Bomb.GetPortCount() + Bomb.GetIndicators().Count()) { ball = true; }
		return ball;
	}
	bool BlueRule()
	{
		bool ball = false;
		if (Bomb.IsIndicatorOn(Indicator.NSA) || Bomb.IsIndicatorOn(Indicator.FRK) || Bomb.IsIndicatorOff(Indicator.MSA) || Bomb.IsIndicatorOff(Indicator.FRQ)) { ball = true; }
		return ball;
	}
	bool GreenRule()
	{
		bool ball = false;
		if (IsOriginalSwitchState(Switches_State)) { ball = true; }
		return ball;
	}
	bool RedRule()
	{
		bool ball = false;
		if ((Bomb.GetStrikes() != 0)||(Bomb.IsTwoFactorPresent())) { ball = true; }
		return ball;
	}
	bool IsMostOneWhite(StringBuilder leds)
	{
		int counter = 0;
		for(int i = 0; i < 10; i++)
		{
			if(leds[i] == 'W') { counter++; }
			if (counter == 2) { return false; }
		}
		return true;
	}
	bool IsUpMoreThanDown(StringBuilder state)
	{
		int up = 0;
		for(int i = 0; i < 5; i++)
		{
			if (state[i] == '1') { up++; }
		}
		if (up > 5 - up) { return true; }
		return false;
	}
	bool IsOriginalSwitchState(StringBuilder state)
	{
		for (int i = 0; i < 10; i++)
		{
			if (state.ToString() == Original_Switches_States[i]) { return true; }
		}
		return false;
	}
	void GenerateLEDS()
	{
		
		for (int i = 0; i < 10; i++)
		{
			int random = Random.Range(0, 8);
			switch (random)
			{
				case 0:
					LEDsColorsString[i] = 'R';
					LEDs[i].GetComponent<MeshRenderer>().material = LEDsColors[0];
					break;
				case 1:
					LEDsColorsString[i] = 'G';
					LEDs[i].GetComponent<MeshRenderer>().material = LEDsColors[1];
					break;
				case 2:
					LEDsColorsString[i] = 'B';
					LEDs[i].GetComponent<MeshRenderer>().material = LEDsColors[2];
					break;
				case 3:
					LEDsColorsString[i] = 'T';
					LEDs[i].GetComponent<MeshRenderer>().material = LEDsColors[3];
					break;
				case 4:
					LEDsColorsString[i] = 'O';
					LEDs[i].GetComponent<MeshRenderer>().material = LEDsColors[4];
					break;
				case 5:
					LEDsColorsString[i] = 'P';
					LEDs[i].GetComponent<MeshRenderer>().material = LEDsColors[5];
					break;
				case 6:
					LEDsColorsString[i] = 'W';
					LEDs[i].GetComponent<MeshRenderer>().material = LEDsColors[6];
					break;
				case 7:
					LEDsColorsString[i] = 'K';
					LEDs[i].GetComponent<MeshRenderer>().material = LEDsColors[7];
					break;
			}
		}
	}
	IEnumerator FlipSwitch(int selected)
	{
		if (Switches_State[selected] == '0') Switches_State[selected] = '1';
		else Switches_State[selected] = '0';
		const float duration = .3f;
		var startTime = Time.fixedTime;
		if (Switches[selected].transform.localEulerAngles.x >= 50 && Switches[selected].transform.localEulerAngles.x <= 60)
		{
			do
			{
				Switches[selected].transform.localEulerAngles = new Vector3(easeOutSine(Time.fixedTime - startTime, duration, 55f, -55f), 0, 0);
				yield return null;
			}
			while (Time.fixedTime < startTime + duration);
			Switches[selected].transform.localEulerAngles = new Vector3(-55f, 0, 0);
		}
		else
		{
			do
			{
				Switches[selected].transform.localEulerAngles = new Vector3(easeOutSine(Time.fixedTime - startTime, duration, -55f, 55f), 0, 0);
				yield return null;
			}
			while (Time.fixedTime < startTime + duration);
			Switches[selected].transform.localEulerAngles = new Vector3(55f, 0, 0);
		}

	}
	private float easeOutSine(float time, float duration, float from, float to)
	{
		return (to - from) * Mathf.Sin(time / duration * (Mathf.PI / 2)) + from;
	}

	//twitch plays
#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} 1 2 3 4 5 [Toggles the specified switches where 1 is leftmost and 5 is rightmost]";
#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
	{
		string[] parameters = command.Split(' ');
		bool extraitem = false;
		if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(parameters[0], @"^\s*toggle\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(parameters[0], @"^\s*switch\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(parameters[0], @"^\s*flip\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			extraitem = true;
			if (parameters.Length == 1)
			{
				yield return "sendtochaterror Please specify the switches that need to be flipped!";
				yield break;
			}
		}
		string[] valids = { "1", "2", "3", "4", "5" };
		if (extraitem)
		{
			for (int i = 1; i < parameters.Length; i++)
			{
				if (!valids.Contains(parameters[i]))
				{
					yield return "sendtochaterror The specified switch '" + parameters[i] + "' is invalid!";
					yield break;
				}
			}
			yield return null;
			for (int i = 1; i < parameters.Length; i++)
			{
				int temp = 0;
				int.TryParse(parameters[i], out temp);
				temp -= 1;
				Switches[temp].GetComponent<KMSelectable>().OnInteract();
				yield return new WaitForSeconds(0.2f);
			}
		}
		else
		{
			for (int i = 0; i < parameters.Length; i++)
			{
				if (!valids.Contains(parameters[i]))
				{
					yield return "sendtochaterror The specified switch '" + parameters[i] + "' is invalid!";
					yield break;
				}
			}
			yield return null;
			for (int i = 0; i < parameters.Length; i++)
			{
				int temp = 0;
				int.TryParse(parameters[i], out temp);
				temp -= 1;
				Switches[temp].GetComponent<KMSelectable>().OnInteract();
				yield return new WaitForSeconds(0.2f);
			}
		}
	}
}
