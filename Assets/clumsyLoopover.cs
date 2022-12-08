using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using rnd = UnityEngine.Random;

public class clumsyLoopover : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    public Renderer[] tiles;
    public Color[] tileColors;
    public TextMesh[] tileLetters;
    public KMSelectable[] negvertibuttons;
    public KMSelectable[] posvertibuttons;
    public KMSelectable[] poshoributtons;
    public KMSelectable[] neghoributtons;

    sealed class shift
    {
        public string[] NextState;
        public string[] PreviousState;
        public bool Row;
        public int Index;
        public int OtherIndex;
        public int Direction;
    }
    private string[] currentState;
    private bool rowAbove;
    private bool colLeft;
    private static readonly string[] solveState = new string[36] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

    private readonly Queue<shift> _animationQueue = new Queue<shift>();
    private List<shift> allMoves = new List<shift>();

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    KMSelectable.OnInteractHandler buttonHandler(bool row, int index, int direction, KMSelectable arrow, Action<KMSelectable, string[]> method)
    {
        return delegate ()
        {
            if (!moduleSolved)
            {
                audio.PlaySoundAtTransform("tick", arrow.transform);
                var prevState = currentState.ToArray();
                method(arrow, currentState);
                var otherIndex = 0;
                KMSelectable otherArrow = null;
                if (row && direction == 1)
                {
                    otherIndex = rowAbove ? (index + 5) % 6 : (index + 7) % 6;
                    otherArrow = poshoributtons[otherIndex];
                }
                else if (row && direction == -1)
                {
                    otherIndex = rowAbove ? (index + 5) % 6 : (index + 7) % 6;
                    otherArrow = neghoributtons[otherIndex];
                }
                else if (!row && direction == -1)
                {
                    otherIndex = colLeft ? (index + 5) % 6 : (index + 7) % 6;
                    otherArrow = posvertibuttons[otherIndex];
                }
                else if (!row && direction == 1)
                {
                    otherIndex = colLeft ? (index + 5) % 6 : (index + 7) % 6;
                    otherArrow = negvertibuttons[otherIndex];
                }
                method(otherArrow, currentState);
                var currentShift = new shift { Row = row, Index = index, OtherIndex = otherIndex, Direction = direction, NextState = currentState.ToArray(), PreviousState = prevState };
                _animationQueue.Enqueue(currentShift);
                allMoves.Add(currentShift);
            }
            return false;
        };
    }

    private void Awake()
    {
        moduleId = moduleIdCounter++;
        rowAbove = rnd.Range(0, 2) == 0;
        colLeft = rnd.Range(0, 2) == 0;
        Debug.LogFormat("[Clumsy Loopover #{0}] Every row button will also move the row {1} it.", moduleId, rowAbove ? "above" : "below");
        Debug.LogFormat("[Clumsy Loopover #{0}] Every column  button will also move the column {1} of it.", moduleId, colLeft ? "left" : "right");

        for (int i = 0; i < negvertibuttons.Length; i++)
            negvertibuttons[i].OnInteract += buttonHandler(false, i, 1, negvertibuttons[i], negvertiPress);
        for (int i = 0; i < posvertibuttons.Length; i++)
            posvertibuttons[i].OnInteract += buttonHandler(false, i, -1, posvertibuttons[i], posvertiPress);
        for (int i = 0; i < poshoributtons.Length; i++)
            poshoributtons[i].OnInteract += buttonHandler(true, i, 1, poshoributtons[i], poshoriPress);
        for (int i = 0; i < neghoributtons.Length; i++)
            neghoributtons[i].OnInteract += buttonHandler(true, i, -1, neghoributtons[i], neghoriPress);
        StartCoroutine(animate());
    }

    private IEnumerator animate()
    {
        while (!moduleSolved)
        {
            while (_animationQueue.Count == 0)
                yield return null;

            var item = _animationQueue.Dequeue();

            const float duration = .15f;
            float elapsed = 0;
            if (item.Direction == -1)
            {
                setBoard(item.NextState);
                while (elapsed < duration)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        var tileToMove = tiles[item.Row ? (6 * item.Index + i) : (item.Index + 6 * i)];
                        var newPosition = new Vector3(
                            -4f + (item.Row ? i : item.Index) * 2.2f + (item.Row ? 2.2f * elapsed / duration - 2.2f : 0) * item.Direction,
                            2.05f - (item.Row ? item.Index : i) * 2.2f + (item.Row ? 0 : 2.2f * elapsed / duration - 2.2f) * item.Direction,
                            0);
                        tileToMove.transform.localPosition = newPosition;
                    }
                    for (int i = 0; i < 6; i++)
                    {
                        var tileToMove = tiles[item.Row ? (6 * item.OtherIndex + i) : (item.OtherIndex + 6 * i)];
                        var newPosition = new Vector3(
                            -4f + (item.Row ? i : item.OtherIndex) * 2.2f + (item.Row ? 2.2f * elapsed / duration - 2.2f : 0) * item.Direction,
                            2.05f - (item.Row ? item.OtherIndex : i) * 2.2f + (item.Row ? 0 : 2.2f * elapsed / duration - 2.2f) * item.Direction,
                            0);
                        tileToMove.transform.localPosition = newPosition;
                    }
                    yield return null;
                    elapsed += Time.deltaTime;
                }
            }
            else
            {
                while (elapsed < duration)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        var tileToMove = tiles[item.Row ? (6 * item.Index + i) : (item.Index + 6 * i)];
                        var newPosition = new Vector3(
                            -4f + (item.Row ? i : item.Index) * 2.2f + (item.Row ? 2.2f * elapsed / duration : 0) * item.Direction,
                            2.05f - (item.Row ? item.Index : i) * 2.2f + (item.Row ? 0 : 2.2f * elapsed / duration) * item.Direction,
                            0);
                        tileToMove.transform.localPosition = newPosition;
                    }
                    for (int i = 0; i < 6; i++)
                    {
                        var tileToMove = tiles[item.Row ? (6 * item.OtherIndex + i) : (item.OtherIndex + 6 * i)];
                        var newPosition = new Vector3(
                            -4f + (item.Row ? i : item.OtherIndex) * 2.2f + (item.Row ? 2.2f * elapsed / duration : 0) * item.Direction,
                            2.05f - (item.Row ? item.OtherIndex : i) * 2.2f + (item.Row ? 0 : 2.2f * elapsed / duration) * item.Direction,
                            0);
                        tileToMove.transform.localPosition = newPosition;
                    }
                    yield return null;
                    elapsed += Time.deltaTime;
                }
                setBoard(item.NextState);
            }

            for (var i = 0; i < 36; i++)
                tiles[i].transform.localPosition = new Vector3(-4f + 2.2f * (i % 6), 2.05f - 2.2f * (i / 6), 0);
        }
    }

    private void Start()
    {
        currentState = solveState.ToArray();
        for (int i = 0; i < 1000; i++)
        {
            var movement = rnd.Range(0, 4);
            var movement2 = rnd.Range(0, 6);
            var movement3 = 0;
            if (movement == 0 || movement == 1)
                movement3 = colLeft ? (movement2 + 5) % 6 : (movement2 + 7) % 6;
            else if (movement == 2 || movement == 3)
                movement3 = rowAbove ? (movement2 + 5) % 6 : (movement2 + 7) % 6;
            var prevState = currentState.ToArray();
            switch (movement)
            {
                case 0:
                    negvertiPress(negvertibuttons[movement2], currentState);
                    negvertiPress(negvertibuttons[movement3], currentState);
                    allMoves.Add(new shift { Row = false, Index = movement2, OtherIndex = movement3, Direction = 1, NextState = currentState.ToArray(), PreviousState = prevState });
                    break;
                case 1:
                    posvertiPress(posvertibuttons[movement2], currentState);
                    posvertiPress(posvertibuttons[movement3], currentState);
                    allMoves.Add(new shift { Row = false, Index = movement2, OtherIndex = movement3, Direction = -1, NextState = currentState.ToArray(), PreviousState = prevState });
                    break;
                case 2:
                    neghoriPress(neghoributtons[movement2], currentState);
                    neghoriPress(neghoributtons[movement3], currentState);
                    allMoves.Add(new shift { Row = true, Index = movement2, OtherIndex = movement3, Direction = -1, NextState = currentState.ToArray(), PreviousState = prevState });
                    break;
                default:
                    poshoriPress(poshoributtons[movement2], currentState);
                    poshoriPress(poshoributtons[movement3], currentState);
                    allMoves.Add(new shift { Row = true, Index = movement2, OtherIndex = movement3, Direction = 1, NextState = currentState.ToArray(), PreviousState = prevState });
                    break;
            }
        }
        setBoard(currentState);
    }

    private void setBoard(string[] state)
    {
        for (int i = 0; i < 36; i++)
        {
            var letter = solveState[i];
            var m = Array.IndexOf(state, letter);
            tiles[m].material.color = tileColors[i];
            tileLetters[m].text = solveState[i];
        }
        if (state.SequenceEqual(solveState))
        {
            module.HandlePass();
            Debug.LogFormat("[Clumsy Loopover #{0}] Module solved!", moduleId);
            audio.PlaySoundAtTransform("solve", transform);
            moduleSolved = true;
        }
    }

    private void negvertiPress(KMSelectable arrow, string[] state)
    {
        var ix = Array.IndexOf(negvertibuttons, arrow);
        var s1 = state[0 + ix];
        var s2 = state[6 + ix];
        var s3 = state[12 + ix];
        var s4 = state[18 + ix];
        var s5 = state[24 + ix];
        var s6 = state[30 + ix];
        state[0 + ix] = s2;
        state[6 + ix] = s3;
        state[12 + ix] = s4;
        state[18 + ix] = s5;
        state[24 + ix] = s6;
        state[30 + ix] = s1;
    }

    private void posvertiPress(KMSelectable arrow, string[] state)
    {
        var ix = Array.IndexOf(posvertibuttons, arrow);
        var s1 = state[0 + ix];
        var s2 = state[6 + ix];
        var s3 = state[12 + ix];
        var s4 = state[18 + ix];
        var s5 = state[24 + ix];
        var s6 = state[30 + ix];
        state[0 + ix] = s6;
        state[6 + ix] = s1;
        state[12 + ix] = s2;
        state[18 + ix] = s3;
        state[24 + ix] = s4;
        state[30 + ix] = s5;
    }

    private void neghoriPress(KMSelectable arrow, string[] state)
    {
        var ix = Array.IndexOf(neghoributtons, arrow);
        var s1 = state[0 + ix * 6];
        var s2 = state[1 + ix * 6];
        var s3 = state[2 + ix * 6];
        var s4 = state[3 + ix * 6];
        var s5 = state[4 + ix * 6];
        var s6 = state[5 + ix * 6];
        state[0 + ix * 6] = s2;
        state[1 + ix * 6] = s3;
        state[2 + ix * 6] = s4;
        state[3 + ix * 6] = s5;
        state[4 + ix * 6] = s6;
        state[5 + ix * 6] = s1;
    }

    private void poshoriPress(KMSelectable arrow, string[] state)
    {
        var ix = Array.IndexOf(poshoributtons, arrow);
        var s1 = state[0 + ix * 6];
        var s2 = state[1 + ix * 6];
        var s3 = state[2 + ix * 6];
        var s4 = state[3 + ix * 6];
        var s5 = state[4 + ix * 6];
        var s6 = state[5 + ix * 6];
        state[0 + ix * 6] = s6;
        state[1 + ix * 6] = s1;
        state[2 + ix * 6] = s2;
        state[3 + ix * 6] = s3;
        state[4 + ix * 6] = s4;
        state[5 + ix * 6] = s5;
    }

    // Twitch Plays
    private bool paramsValid(string[] prms)
    {
        string[] validsRows = { "row1", "row2", "row3", "row4", "row5", "row6" };
        string[] validsCols = { "col1", "col2", "col3", "col4", "col5", "col6" };
        string[] validsLeftsRights = { "l1", "l2", "l3", "l4", "l5", "l6", "r1", "r2", "r3", "r4", "r5", "r6" };
        string[] validsUpsDowns = { "u1", "u2", "u3", "u4", "u5", "u6", "d1", "d2", "d3", "d4", "d5", "d6" };
        if (prms.Length % 2 != 0)
        {
            return false;
        }
        for (int i = 1; i < prms.Length; i += 2)
        {
            if (!validsCols.Contains(prms[i - 1]) && !validsRows.Contains(prms[i - 1]))
            {
                return false;
            }
            if (validsCols.Contains(prms[i - 1]))
            {
                if (!validsUpsDowns.Contains(prms[i]))
                {
                    return false;
                }
            }
            else if (validsRows.Contains(prms[i - 1]))
            {
                if (!validsLeftsRights.Contains(prms[i]))
                {
                    return false;
                }
            }
        }
        return true;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} row<#> l/r<#2> [Moves left/right '#2' times in row '#'] | !{0} col<#> u/d<#2> [Moves up/down '#2' times in column '#'] | Commands are chainable, for ex: !{0} row1 l3 col1 d1";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (paramsValid(parameters))
        {
            yield return null;
            for (int i = 0; i < parameters.Length - 1; i++)
            {
                if (parameters[i].EqualsIgnoreCase("col1"))
                {
                    int temp;
                    int.TryParse(parameters[i + 1].Substring(1, 1), out temp);
                    if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("u"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            negvertibuttons[0].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                    else if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("d"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            posvertibuttons[0].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                }
                else if (parameters[i].EqualsIgnoreCase("col2"))
                {
                    int temp;
                    int.TryParse(parameters[i + 1].Substring(1, 1), out temp);
                    if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("u"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            negvertibuttons[1].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                    else if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("d"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            posvertibuttons[1].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                }
                else if (parameters[i].EqualsIgnoreCase("col3"))
                {
                    int temp;
                    int.TryParse(parameters[i + 1].Substring(1, 1), out temp);
                    if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("u"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            negvertibuttons[2].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                    else if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("d"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            posvertibuttons[2].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                }
                else if (parameters[i].EqualsIgnoreCase("col4"))
                {
                    int temp;
                    int.TryParse(parameters[i + 1].Substring(1, 1), out temp);
                    if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("u"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            negvertibuttons[3].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                    else if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("d"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            posvertibuttons[3].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                }
                else if (parameters[i].EqualsIgnoreCase("col5"))
                {
                    int temp;
                    int.TryParse(parameters[i + 1].Substring(1, 1), out temp);
                    if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("u"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            negvertibuttons[4].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                    else if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("d"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            posvertibuttons[4].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                }
                else if (parameters[i].EqualsIgnoreCase("col6"))
                {
                    int temp;
                    int.TryParse(parameters[i + 1].Substring(1, 1), out temp);
                    if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("u"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            negvertibuttons[5].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                    else if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("d"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            posvertibuttons[5].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                }
                else if (parameters[i].EqualsIgnoreCase("row1"))
                {
                    int temp;
                    int.TryParse(parameters[i + 1].Substring(1, 1), out temp);
                    if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("l"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            neghoributtons[0].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                    else if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("r"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            poshoributtons[0].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                }
                else if (parameters[i].EqualsIgnoreCase("row2"))
                {
                    int temp;
                    int.TryParse(parameters[i + 1].Substring(1, 1), out temp);
                    if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("l"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            neghoributtons[1].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                    else if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("r"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            poshoributtons[1].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                }
                else if (parameters[i].EqualsIgnoreCase("row3"))
                {
                    int temp;
                    int.TryParse(parameters[i + 1].Substring(1, 1), out temp);
                    if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("l"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            neghoributtons[2].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                    else if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("r"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            poshoributtons[2].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                }
                else if (parameters[i].EqualsIgnoreCase("row4"))
                {
                    int temp;
                    int.TryParse(parameters[i + 1].Substring(1, 1), out temp);
                    if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("l"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            neghoributtons[3].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                    else if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("r"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            poshoributtons[3].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                }
                else if (parameters[i].EqualsIgnoreCase("row5"))
                {
                    int temp;
                    int.TryParse(parameters[i + 1].Substring(1, 1), out temp);
                    if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("l"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            neghoributtons[4].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                    else if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("r"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            poshoributtons[4].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                }
                else if (parameters[i].EqualsIgnoreCase("row6"))
                {
                    int temp;
                    int.TryParse(parameters[i + 1].Substring(1, 1), out temp);
                    if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("l"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            neghoributtons[5].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                    else if (parameters[i + 1].Substring(0, 1).EqualsIgnoreCase("r"))
                    {
                        for (int j = 0; j < temp; j++)
                        {
                            poshoributtons[5].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                    }
                }
            }
        }
    }
}