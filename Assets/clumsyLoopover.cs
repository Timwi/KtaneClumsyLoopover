using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        if (Regex.IsMatch(command, @"^\s*s\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            var e = TwitchSolver(true);
            while (e.MoveNext())
                yield return e.Current;
        }

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

    private IEnumerator TwitchHandleForcedSolve()
    {
        return TwitchSolver();
    }

    private IEnumerator TwitchSolver(bool oneStepOnly = false)
    {
        var colUp = Enumerable.Range(0, 6).Select(col => negvertibuttons[colLeft ? (col + 1) % 6 : col]).ToArray();
        var colDown = Enumerable.Range(0, 6).Select(col => posvertibuttons[colLeft ? (col + 1) % 6 : col]).ToArray();
        var rowLeft = Enumerable.Range(0, 6).Select(row => neghoributtons[rowAbove ? (row + 1) % 6 : row]).ToArray();
        var rowRight = Enumerable.Range(0, 6).Select(row => poshoributtons[rowAbove ? (row + 1) % 6 : row]).ToArray();

        //foreach (var btn in new[] { colUp[0] })
        //{
        //    btn.OnInteract();
        //    yield return new WaitForSeconds(.1f);
        //}
        //yield break;

        // Step 1: place the bottommost three rows
        while (true)
        {
            var state = currentState.Select(s => s[0] >= '0' && s[0] <= '9' ? s[0] - '0' : s[0] - 'A' + 10).ToArray();
            var cell = Enumerable.Range(0, 36).LastOrDefault(c => state[c] != c);
            if (cell == 0)
                break;
            if (cell < 15)
            {
                Debug.LogFormat("<> temporarily done!");
                yield break;
            }

            var source = Array.IndexOf(state, cell);
            if (source > cell)
                throw new InvalidOperationException();
            if (source == cell)
                continue;

            var buttons = new List<KMSelectable>();
            // Case where the col B cell is already correct but the col A cell is not
            if (cell % 6 == 0)
            {
                Debug.LogFormat("<> Cell {0} is correct but Cell {1} isn’t ⇒ moving {0} out of column B", solveState[cell + 1], solveState[cell]);
                buttons.Add(colUp[0]);
                buttons.Add(rowRight[cell / 6 - 2]);
                buttons.Add(colDown[0]);
            }
            else if (cell % 6 == 1)
            {
                // for columns A,B: find out where both of the next two tiles are
                var src1 = Array.IndexOf(state, cell - 1);
                var src2 = Array.IndexOf(state, cell);

                // Move src1 and src2 out of the top row
                if (src1 / 6 == 0 && src2 / 6 == 1 && (src1 + 2) % 6 == src2 % 6)
                {
                    Debug.LogFormat("<> Moving {0} out of the top row (special case)", solveState[cell - 1]);
                    buttons.Add(colDown[(src1 + 5) % 6]);
                    buttons.Add(rowRight[1]);
                    buttons.Add(colUp[(src1 + 5) % 6]);
                }
                else if (src1 / 6 == 0)
                {
                    Debug.LogFormat("<> Moving {0} out of the top row (common case)", solveState[cell - 1]);
                    buttons.Add(colDown[src1]);
                    buttons.Add(rowLeft[1]);
                    buttons.Add(colUp[src1]);
                }
                else if (src2 / 6 == 0 && src1 / 6 == 1 && (src2 + 2) % 6 == src1 % 6)
                {
                    Debug.LogFormat("<> Moving {0} out of the top row (special case)", solveState[cell]);
                    buttons.Add(colDown[(src2 + 5) % 6]);
                    buttons.Add(rowRight[1]);
                    buttons.Add(colUp[(src2 + 5) % 6]);
                }
                else if (src2 / 6 == 0)
                {
                    Debug.LogFormat("<> Moving {0} out of the top row (common case)", solveState[cell]);
                    buttons.Add(colDown[src2]);
                    buttons.Add(rowLeft[1]);
                    buttons.Add(colUp[src2]);
                }
                // Special case: if the two cells are already in the correct place but swapped
                else if (src1 == cell && src2 == cell - 1)
                {
                    Debug.LogFormat("<> {0} and {1} are correct but swapped ⇒ move them out", solveState[cell - 1], solveState[cell]);
                    buttons.Add(colUp[0]);
                    buttons.Add(rowRight[cell / 6 - 2]);
                    buttons.Add(colDown[0]);
                }
                // Move src1 out of the left two columns
                else if (src1 != cell && src1 % 6 < 2)
                {
                    Debug.LogFormat("<> Moving {0} out of col A/B", solveState[cell - 1]);
                    buttons.Add((src1 % 6 == 0 ? rowLeft : rowRight)[src1 / 6 == cell / 6 - 1 ? src1 / 6 - 1 : src1 / 6]);
                }
                else if (src1 != cell)
                {
                    Debug.LogFormat("<> Moving {0} to position B{1}", solveState[cell - 1], cell / 6 + 1);
                    buttons.AddRange(Enumerable.Repeat(colUp[0], cell / 6 - src1 / 6));
                    buttons.AddRange(Enumerable.Repeat(rowLeft[src1 / 6 - 1], src1 % 6 - 2));
                    if (src2 == src1 - 1 && src2 / 6 == cell / 6 - 1)
                    {
                        buttons.Add(colUp[0]);
                        buttons.Add(rowRight[src2 / 6 - 2]);
                        buttons.Add(colDown[0]);
                    }
                    else if (src2 == src1 - 1)
                    {
                        buttons.Add(colDown[0]);
                        buttons.Add(rowRight[src1 / 6]);
                        buttons.Add(colUp[0]);
                        buttons.Add(rowLeft[src1 / 6 - 1]);
                    }
                    buttons.Add(rowLeft[src1 / 6 - 1]);
                    buttons.AddRange(Enumerable.Repeat(colDown[0], cell / 6 - src1 / 6));
                }
                // Move src2 into column C
                else if (src2 % 6 < 2)
                {
                    Debug.LogFormat("<> Moving {0} right into column C", solveState[cell]);
                    buttons.AddRange(Enumerable.Repeat(rowRight[src2 / 6 - 1], 2 - src2 % 6));
                }
                else if (src2 % 6 > 2)
                {
                    Debug.LogFormat("<> Moving {0} left into column C", solveState[cell]);
                    buttons.AddRange(Enumerable.Repeat(rowLeft[src2 / 6 - 1], src2 % 6 - 2));
                }
                // Final scoop
                else
                {
                    Debug.LogFormat("<> Moving {0} and {1} into their place", solveState[cell - 1], solveState[cell]);
                    buttons.AddRange(Enumerable.Repeat(colUp[0], cell / 6 - src2 / 6));
                    buttons.Add(rowLeft[src2 / 6 - 1]);
                    buttons.AddRange(Enumerable.Repeat(colDown[0], cell / 6 - src2 / 6));
                }
            }
            else
            {
                // for columns C,D,E,F:
                var srcCol = source % 6;
                var srcRow = source / 6;
                var targetCol = cell % 6;
                var targetRow = cell / 6;

                if (srcRow == targetRow)
                {
                    // case ⬡: source is in the target row
                    // move source column-1 up, move target row-2 right×2, move source column back, goto △
                    Debug.LogFormat("<> Case ⬡ for {0}", solveState[cell]);
                    buttons.Add(colUp[srcCol == 0 ? 0 : srcCol - 1]);
                    buttons.Add(rowRight[targetRow - 2]);
                    buttons.Add(rowRight[targetRow - 2]);
                    buttons.Add(colDown[srcCol == 0 ? 0 : srcCol - 1]);
                    srcCol = (srcCol + 2) % 6;
                    srcRow--;
                }

                if ((srcCol > targetCol || srcCol < targetCol - 1) && srcRow > 0)
                {
                    Debug.LogFormat("<> Case △ for {0}", solveState[cell]);
                    goto case1;
                }

                if (srcCol > targetCol || srcCol < targetCol - 1)
                {
                    Debug.LogFormat("<> Case □ for {0}", solveState[cell]);
                    goto case2;
                }

                // case ▽: source is in the target column or target column-1
                // move source row or row-1 (as appropriate) right, goto △ or □
                case3:
                Debug.LogFormat("<> Case ▽ for {0}", solveState[cell]);
                buttons.Add((srcCol == targetCol ? rowRight : rowLeft)[srcRow == 0 ? 0 : srcRow - 1]);
                srcCol += srcCol == targetCol ? 1 : -1;
                if (srcRow != 0)
                    goto case1;

                // case □: source is in the top row and not in the target column or target column-1
                // move target column-1 up, move source row left into target column, move target column back, goto ▽
                case2:
                buttons.Add(colUp[targetCol - 1]);
                buttons.AddRange(Enumerable.Repeat(rowLeft[srcRow], (srcCol + 6 - targetCol) % 6));
                buttons.Add(colDown[targetCol - 1]);
                srcRow++;
                srcCol = targetCol;
                goto case3;

                case1:
                // case △: source is not in the top row or in the target column or target column-1
                // move target column-1 up, move source row-1 left, move target column back
                buttons.AddRange(Enumerable.Repeat(colUp[targetCol - 1], targetRow - srcRow));
                buttons.AddRange(Enumerable.Repeat(rowLeft[srcRow - 1], (srcCol + 6 - targetCol) % 6));
                buttons.AddRange(Enumerable.Repeat(colDown[targetCol - 1], targetRow - srcRow));
            }

            Debug.LogFormat("<> Buttons: {0}", buttons.Select(btn =>
            {
                if (colUp.Contains(btn))
                    return "↑" + (char) ('A' + Array.IndexOf(colUp, btn));
                else if (colDown.Contains(btn))
                    return "↓" + (char) ('A' + Array.IndexOf(colDown, btn));
                else if (rowRight.Contains(btn))
                    return "→" + (Array.IndexOf(rowRight, btn) + 1);
                else if (rowLeft.Contains(btn))
                    return "←" + (Array.IndexOf(rowLeft, btn) + 1);
                return "ERROR";
            }).Join(" "));

            foreach (var btn in buttons)
            {
                btn.OnInteract();
                yield return new WaitForSeconds(.1f);
            }
            while (_animationQueue.Count > 0)
                yield return true;

            if (oneStepOnly)
                break;
        }

        yield break;
    }
}