using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class BoardStateMultiplayer : MonoBehaviour
{
    public Tilemap currentState;
    public Tilemap nextState;
    public Tile playerOneTile;
    public Tile playerTwoTile;
    public Tile borderTile;
    public int limit;

    public Color runningColor;
    public Color pauseFirstColor;
    public Color pauseSecondColor;

    public int startingCredits = 50;
    public float updateInterval = 0.5f;
    private bool running = false;
    private bool[] pause = new bool[2]{true, false};

    private int[] points = new int[2]{0, 0};
    private int[] credits = new int[2]{0, 0};

    private int iter = 0;

    // private readonly HashSet<Vector3Int> aliveFirstCells = new();
    // private readonly HashSet<Vector3Int> aliveSecondCells = new();
    private readonly HashSet<Vector3Int> placedThisPause = new();
    private readonly HashSet<Vector3Int> toCheckCells = new();
    private Tile[] playerTiles;
    private HashSet<Vector3Int>[] playerCells;

    private Camera mainCamera;

    // Start is called before the first frame update
    private IEnumerator Start()
    {
        mainCamera = Camera.main;
        credits[0] = startingCredits;
        credits[1] = startingCredits;
        playerTiles = new Tile[3]{playerOneTile, playerTwoTile, borderTile};
        playerCells = new HashSet<Vector3Int>[3]{new(), new(), new()};

        for (int i = -limit - 1; i <= limit + 1; i++) {
            Vector3Int[] coords = new Vector3Int[4]{new Vector3Int(i, -limit - 1), new Vector3Int(i, limit + 1), new Vector3Int(-limit - 1, i), new Vector3Int(limit + 1, i)};
            for (int j = 0; j < 4; j++) {
                currentState.SetTile(coords[j], playerTiles[2]);
                playerCells[2].Add(coords[j]);
            }

        }

        while (true) {
            if (running) {
                iter += 1;
                UpdateTick();
                if (iter % 100 == 0) {
                    credits[0] += 50;
                    credits[0] += 50;
                }
                if (iter == 9999) {
                    SceneManager.LoadScene(0);
                }
            }
            yield return new WaitForSeconds(updateInterval);
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            SceneManager.LoadScene(0);
        }
        if (Input.GetKeyDown(KeyCode.Space)) {
            // Change of pause state, looks awful, but this is my way
            if (running) {
                running = false;
                pause[0] = true;
            } else if (pause[0]) {
                pause[0] = false;
                pause[1] = true;
            } else {
                pause[1] = false;
                running = true;
            }
            placedThisPause.Clear();
            mainCamera.backgroundColor = running ? runningColor : (pause[0] ? pauseFirstColor : pauseSecondColor);
        }
        if (!running && Input.GetMouseButtonDown(0)) {
            MouseUpdateCell();
        }
    }

    private void MouseUpdateCell() {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cell = currentState.WorldToCell(mouseWorldPos);
        int player = CheckPlayer(cell);
        if (player == 0) {
            if (pause[0]) {
                currentState.SetTile(cell, playerTiles[0]);
                placedThisPause.Add(cell);
                credits[0] -= 1;
                playerCells[0].Add(cell);
                return;
            }
            currentState.SetTile(cell, playerTiles[1]);
            placedThisPause.Add(cell);
            credits[1] -= 1;
            playerCells[1].Add(cell);
            return;
        }
        if (pause[player - 1]) {
            currentState.SetTile(cell, null);
            if (placedThisPause.Contains(cell)) {
                credits[player - 1] += 1;
            }
            playerCells[player - 1].Remove(cell);
        }
    }

    private void UpdateTick() {
        toCheckCells.Clear();

        for (int i = 0; i < 2; i++) {
            foreach (Vector3Int cell in playerCells[i]) {
                for (int x = -1; x <= 1; x++) {
                    for (int y = -1; y <= 1; y++) {
                        toCheckCells.Add(cell + new Vector3Int(x, y));
                    }
                }
            }
        }
        foreach (Vector3Int cell in playerCells[2]) {
            toCheckCells.Add(cell);
        }
        
        foreach (Vector3Int cell in toCheckCells) {
            if (Mathf.Abs(cell.x) >= limit + 1 || Mathf.Abs(cell.y) >= limit + 1) {
                nextState.SetTile(cell, borderTile);
                continue;
            }

            CheckPlayerCell(cell, CheckPlayer(cell));
        }

        (currentState, nextState) = (nextState, currentState);
        nextState.ClearAllTiles();
    }

    private void NormalLogic(Vector3Int cell, int player, int neighbours, int check) {
        if (player == 0 && neighbours == 3) {
            nextState.SetTile(cell, playerTiles[check - 1]);
            if (currentState.GetTile(cell) != playerTiles[check - 1]) {
                points[check - 1] += 1;
            }
            playerCells[check - 1].Add(cell);
            return;
        }
        if (player == check && (neighbours < 2 || neighbours > 3)) {
            nextState.SetTile(cell, null);
            playerCells[check - 1].Remove(cell);
            return;
        }
        nextState.SetTile(cell, (player == check ? playerTiles[check - 1] : null));
        return;
    }

    private void CheckPlayerCell(Vector3Int cell, int player) {
        int firstNeighbours = Count(cell, 1);
        int secondNeighbours = Count(cell, 2);
        if (secondNeighbours == 0) {
            NormalLogic(cell, player, firstNeighbours, 1);
            return;
        }
        if (firstNeighbours == 0) {
            NormalLogic(cell, player, secondNeighbours, 2);
            return;
        }
        if (player == 0) {
            if (firstNeighbours > secondNeighbours) {
                nextState.SetTile(cell, playerTiles[0]);
                if (currentState.GetTile(cell) != playerTiles[0]) {
                    points[0] += 1;
                }
                playerCells[0].Add(cell);
                return;
            }
            if (secondNeighbours > firstNeighbours) {
                nextState.SetTile(cell, playerTiles[1]);
                if (currentState.GetTile(cell) != playerTiles[1]) {
                    points[1] += 1;
                }
                playerCells[1].Add(cell);
                return;
            }
            nextState.SetTile(cell, (Random.value <= 0.5f ? playerTiles[0] : playerTiles[1]));
            return;
        }
        int enemy = (player == 1 ? 2 : 1);
        firstNeighbours = Count(cell, enemy);
        secondNeighbours = Count(cell, player);
        if (firstNeighbours > secondNeighbours) {
            nextState.SetTile(cell, playerTiles[enemy - 1]);
            if (currentState.GetTile(cell) != playerTiles[enemy - 1]) {
                points[enemy - 1] += 1;
            }
            playerCells[enemy - 1].Add(cell);
            return;
        }
        if (secondNeighbours > firstNeighbours) {
            nextState.SetTile(cell, playerTiles[player - 1]);
            if (currentState.GetTile(cell) != playerTiles[player - 1]) {
                points[player - 1] += 1;
            }
            playerCells[player - 1].Add(cell);
            return;
        }
        nextState.SetTile(cell, playerTiles[player - 1]);
    }

    private int CheckPlayer(Vector3Int cell) {
        TileBase check = currentState.GetTile(cell);
        return (check == playerOneTile ? 1 : (check == playerTwoTile ? 2 : 0));
    }

    private int Count(Vector3Int cell, int player) {
        int count = 0;
        
        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                if (x == y && x == 0) {
                    continue;
                }
                count += (CheckPlayer(cell + new Vector3Int(x, y)) == player ? 1 : 0);
            }
        }
        
        return count;
    }
}
