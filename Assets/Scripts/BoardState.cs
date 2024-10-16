using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class BoardState : MonoBehaviour
{
    public Tilemap currentState;
    public Tilemap nextState;
    public Tile aliveTile;

    public Color runningColor;
    public Color pauseColor;

    public bool isBackground;

    public float updateInterval = 0.5f;
    private bool running = false;

    private readonly HashSet<Vector3Int> aliveCells = new();
    private readonly HashSet<Vector3Int> toCheckCells = new();

    private Camera mainCamera;

    // Start is called before the first frame update
    private IEnumerator Start()
    {
        mainCamera = Camera.main;

        if (isBackground) {
            RandomizeBoard();
            running = true;
        }

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
        if (isBackground) {
            return;
        }
        if (Input.GetKeyDown(KeyCode.Escape)) {
            SceneManager.LoadScene(0);
        }
        if (Input.GetKeyDown(KeyCode.Space)) {
            running = !running;

            mainCamera.backgroundColor = running ? runningColor : pauseColor;
        }
        if (!running && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) {
            MouseUpdateCell();
        }
        if (!running && Input.GetKeyDown(KeyCode.R)) {
            RandomizeBoard();
        }
        if (!running && Input.GetKeyDown(KeyCode.C)) {
            ClearBoard();
        }
    }

    private void MouseUpdateCell() {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cell = currentState.WorldToCell(mouseWorldPos);
        if (IsAlive(cell)) {
            currentState.SetTile(cell, null);
            aliveCells.Remove(cell);
            return;
        }
        currentState.SetTile(cell, aliveTile);
        aliveCells.Add(cell);
    }

    private void RandomizeBoard() {
        ClearBoard();
        for (int i = -50; i < 50; i++) {
            for (int j = -50; j < 50; j++) {
                if (Random.value <= 0.5f) {
                    continue;
                }
                Vector3Int cell = new Vector3Int(i, j);
                currentState.SetTile(cell, aliveTile);
                aliveCells.Add(cell);
            }
        }
    }

    private void ClearBoard() {
        currentState.ClearAllTiles();
        nextState.ClearAllTiles();
        aliveCells.Clear();
        toCheckCells.Clear();
    }

    private void UpdateTick() {
        toCheckCells.Clear();

        foreach (Vector3Int cell in aliveCells) {
            for (int x = -1; x <= 1; x++) {
                for (int y = -1; y <= 1; y++) {
                    toCheckCells.Add(cell + new Vector3Int(x, y));
                }
            }
        }

        foreach (Vector3Int cell in toCheckCells) {
            if (Mathf.Abs(cell.x) >= 101 || Mathf.Abs(cell.y) >= 101) {
                nextState.SetTile(cell, null);
                continue;
            }

            int neighbors = Count(cell);
            bool alive = IsAlive(cell);

            if (!alive && neighbors == 3) {
                nextState.SetTile(cell, aliveTile);
                aliveCells.Add(cell);
                continue;
            }
            if (alive && (neighbors < 2 || neighbors > 3)) {
                nextState.SetTile(cell, null);
                aliveCells.Remove(cell);
                continue;
            }
            nextState.SetTile(cell, (alive ? aliveTile : null));
        }

        (currentState, nextState) = (nextState, currentState);
        nextState.ClearAllTiles();
    }

    private bool IsAlive(Vector3Int cell) {
        return currentState.GetTile(cell) == aliveTile;
    }

    private int Count(Vector3Int cell) {
        int count = 0;
        
        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                if (x == y && x == 0) {
                    continue;
                }
                count += (IsAlive(cell + new Vector3Int(x, y)) ? 1 : 0);
            }
        }
        
        return count;
    }
}
