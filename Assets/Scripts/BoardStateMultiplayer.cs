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

    public Color runningColor;
    public Color pauseFirstColor;
    public Color pauseSecondColor;

    public float updateInterval = 0.5f;
    private bool running = false;
    private bool pauseFirst = true;
    private bool pauseSecond = false;

    private readonly HashSet<Vector3Int> aliveFirstCells = new();
    private readonly HashSet<Vector3Int> aliveSecondCells = new();
    private readonly HashSet<Vector3Int> toCheckCells = new();
    private Tile[] playerTiles;
    private HashSet<Vector3Int>[] playerCells;

    private Camera mainCamera;

    // Start is called before the first frame update
    private IEnumerator Start()
    {
        mainCamera = Camera.main;
        playerTiles = new Tile[2]{playerOneTile, playerTwoTile};
        playerCells = new HashSet<Vector3Int>[2]{aliveFirstCells, aliveSecondCells};

        while (true) {
            if (running) {
                UpdateTick();
            }
            yield return new WaitForSeconds(updateInterval);
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            SceneManager.LoadScene(1);
        }
        if (Input.GetKeyDown(KeyCode.Space)) {
            if (running) {
                running = false;
                pauseFirst = true;
            } else if (pauseFirst) {
                pauseFirst = false;
                pauseSecond = true;
            } else {
                pauseSecond = false;
                running = true;
            }
            mainCamera.backgroundColor = running ? runningColor : (pauseFirst ? pauseFirstColor : pauseSecondColor);
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
            if (pauseFirst) {
                currentState.SetTile(cell, playerOneTile);
                aliveFirstCells.Add(cell);
            }
            currentState.SetTile(cell, playerTwoTile);
            aliveSecondCells.Add(cell);
            return;
        }
        if (player == 1 && pauseFirst) {
            currentState.SetTile(cell, null);
            aliveFirstCells.Remove(cell);
            return;
        }
        if (player == 2 && pauseSecond) {
            currentState.SetTile(cell, null);
            aliveSecondCells.Remove(cell);
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
        

        foreach (Vector3Int cell in toCheckCells) {
            if (Mathf.Abs(cell.x) >= 101 || Mathf.Abs(cell.y) >= 101) {
                nextState.SetTile(cell, null);
                continue;
            }

            CheckPlayerCell(cell, CheckPlayer(cell));
        }

        (currentState, nextState) = (nextState, currentState);
        nextState.ClearAllTiles();
    }

    private void CheckPlayerCell(Vector3Int cell, int player) {
        int firstNeighbours = Count(cell, 1);
        int secondNeighbours = Count(cell, 2);
        if (player == 0) {
            if (firstNeighbours > secondNeighbours) {
                nextState.SetTile(cell, playerOneTile);
                playerCells[0].Add(cell);
                return;
            }
            if (secondNeighbours > firstNeighbours) {
                nextState.SetTile(cell, playerTwoTile);
                playerCells[1].Add(cell);
                return;
            }
            nextState.SetTile(cell, null);
            return;
        }
        int enemy = (player == 1 ? 2 : 1);
        firstNeighbours = Count(cell, enemy);
        secondNeighbours = Count(cell, player);
        if (firstNeighbours > secondNeighbours) {
            nextState.SetTile(cell, playerTiles[enemy]);
            playerCells[enemy - 1].Add(cell);
            return;
        }
        if (secondNeighbours > firstNeighbours) {
            nextState.SetTile(cell, (player == 1 ? playerOneTile : playerTwoTile));
            playerCells[player - 1].Add(cell);
            return;
        }
        nextState.SetTile(cell, null);
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
