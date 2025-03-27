using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Match3 : MonoBehaviour
{
    [SerializeField] int width = 8;
    [SerializeField] int height = 8;
    [SerializeField] float cellSize = 1f;
    [SerializeField] Vector3 originPosition = Vector3.zero;
    [SerializeField] bool debug = true;

    [SerializeField] Gem gemPrefab;
    [SerializeField] GemType[] gemTypes;
    [SerializeField] Ease ease = Ease.InQuad;
    [SerializeField] GameObject explosion;
    private bool isAnimating = false;

    InputReader inputReader;
    AudioManager audioManager;
    [SerializeField] private ExplosionVFXPool explosionPool;
    [SerializeField] private ScoreManager scoreManager;

    GridSystem2D<GridObject<Gem>> grid;

    Vector2Int selectedGem = Vector2Int.one * -1;

    void Awake()
    {
        inputReader = GetComponent<InputReader>();
        audioManager = GetComponent<AudioManager>();
    }

    void Start()
    {
        InitializeGrid();
        inputReader.Fire += OnSelectGem;

    }

    void OnDestroy()
    {
        inputReader.Fire -= OnSelectGem;
    }

    //Select gem method
    void OnSelectGem()
    {
        if (isAnimating) return;

        var gridPos = grid.GetXY(Camera.main.ScreenToWorldPoint(inputReader.Selected));

        if (!IsValidPosition(gridPos) || IsEmptyPosition(gridPos)) return;

        if (selectedGem == gridPos)
        {
            DeselectGem();
            audioManager.PlayDeselect();
        }
        else if (selectedGem == Vector2Int.one * -1)
        {
            SelectGem(gridPos);
            audioManager.PlayClick();
        }
        else
        {
            if (IsAdjacent(selectedGem, gridPos))
            {
                StartCoroutine(RunGameLoop(selectedGem, gridPos));
            }
            else
            {
                SelectGem(gridPos);
                audioManager.PlayClick();
            }
        }
    }



    //Checks two adjacent positions
    bool IsAdjacent(Vector2Int posA, Vector2Int posB)
    {
        return Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.y - posB.y) == 1;
    }

    //Game loop to check matches, explode gems and fill spaces
    IEnumerator RunGameLoop(Vector2Int gridPosA, Vector2Int gridPosB)
    {
        if (isAnimating) yield break;
        isAnimating = true;

        yield return StartCoroutine(SwapGems(gridPosA, gridPosB));

        Vector2Int previousA = gridPosA;
        Vector2Int previousB = gridPosB;

        gridPosA = Vector2Int.one * -1;
        gridPosB = Vector2Int.one * -1;

        while (true)
        {
            List<Vector2Int> matches = FindMatches();

            if (matches.Count == 0)
            {
                // Se não houver match na jogada inicial, desfazemos a troca
                if (previousA != previousB)
                {
                    yield return StartCoroutine(SwapGems(previousA, previousB));
                    audioManager.PlayNoMatch();
                }
                break; // Sai do loop
            }

            yield return StartCoroutine(ExplodeGems(matches));
            yield return StartCoroutine(MakeGemsFall());
            yield return StartCoroutine(FillEmptySpots());

            previousA = previousB = Vector2Int.one * -1;
        }
        yield return new WaitForSeconds(0.5f);

        isAnimating = false;

        DeselectGem();
    }

    //Coroutine to spawn new gems on empty spots
    IEnumerator FillEmptySpots()
    {
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (grid.GetValue(x, y) == null)
                {
                    CreateGem(x, y);
                    audioManager.PlayPop();
                    yield return new WaitForSeconds(0.07f);
                }
            }
        }
    }

    //Coroutine to make gems fall on empty spaces below
    IEnumerator MakeGemsFall()
    {
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                if (grid.GetValue(x, y) == null)
                {
                    for (var i = y + 1; i < height; i++)
                    {
                        if (grid.GetValue(x, i) != null)
                        {
                            var gem = grid.GetValue(x, i).GetValue();
                            grid.SetValue(x, y, grid.GetValue(x, i));
                            grid.SetValue(x, i, null);
                            gem.transform
                                .DOLocalMove(grid.GetWorldPositionCenter(x, y), 0.5f)
                                .SetEase(ease);
                            audioManager.PlayWoosh();
                            yield return new WaitForSeconds(0.07f);
                            break;
                        }
                    }
                }
            }
        }
    }

    //Method to explode matching gems and spawn VFX
    IEnumerator ExplodeGems(List<Vector2Int> matches)
    {
        audioManager.PlayPop();

        foreach (var match in matches)
        {
            var gem = grid.GetValue(match.x, match.y).GetValue();
            grid.SetValue(match.x, match.y, null);

            ExplodeVFX(match);

            scoreManager.AddPoints(scoreManager.pointsPerMatch);

            gem.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f, 1, 0.5f);

            yield return new WaitForSeconds(0.1f);

            Destroy(gem.gameObject, 0.1f);
        }
    }

    //Puts the explosion vfx on the match
    void ExplodeVFX(Vector2Int match)
    {
        GameObject fx = explosionPool.GetExplosion();

        fx.transform.position = grid.GetWorldPositionCenter(match.x, match.y);

        StartCoroutine(ReturnToPoolAfterTime(fx, 5f));
    }

    //return the fx to the pool (if not null)
    private IEnumerator ReturnToPoolAfterTime(GameObject fx, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (fx != null)
        {
            explosionPool.ReturnExplosion(fx);
        }
    }


    // Checks for matches horizontaly and verticaly
    List<Vector2Int> FindMatches()
    {
        HashSet<Vector2Int> matches = new();

        // Horizontal
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width - 2; x++)
            {
                var gemA = grid.GetValue(x, y);
                var gemB = grid.GetValue(x + 1, y);
                var gemC = grid.GetValue(x + 2, y);

                if (gemA == null || gemB == null || gemC == null) continue;

                if (gemA.GetValue().GetType() == gemB.GetValue().GetType()
                    && gemB.GetValue().GetType() == gemC.GetValue().GetType())
                {
                    matches.Add(new Vector2Int(x, y));
                    matches.Add(new Vector2Int(x + 1, y));
                    matches.Add(new Vector2Int(x + 2, y));
                }
            }
        }

        // Vertical
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height - 2; y++)
            {
                var gemA = grid.GetValue(x, y);
                var gemB = grid.GetValue(x, y + 1);
                var gemC = grid.GetValue(x, y + 2);

                if (gemA == null || gemB == null || gemC == null) continue;

                if (gemA.GetValue().GetType() == gemB.GetValue().GetType()
                    && gemB.GetValue().GetType() == gemC.GetValue().GetType())
                {
                    matches.Add(new Vector2Int(x, y));
                    matches.Add(new Vector2Int(x, y + 1));
                    matches.Add(new Vector2Int(x, y + 2));
                }
            }
        }

        if (matches.Count != 0)
        {
            audioManager.PlayMatch();
        }


        return new List<Vector2Int>(matches);
    }

    //Coroutine to swap 2 gems (adjacent gems only)
    IEnumerator SwapGems(Vector2Int gridPosA, Vector2Int gridPosB)
    {
        var gridObjectA = grid.GetValue(gridPosA.x, gridPosA.y);
        var gridObjectB = grid.GetValue(gridPosB.x, gridPosB.y);

        // Animar a troca
        gridObjectA.GetValue().transform
            .DOLocalMove(grid.GetWorldPositionCenter(gridPosB.x, gridPosB.y), 0.5f)
            .SetEase(ease);
        gridObjectB.GetValue().transform
            .DOLocalMove(grid.GetWorldPositionCenter(gridPosA.x, gridPosA.y), 0.5f)
            .SetEase(ease);

        var gem = grid.GetValue(selectedGem.x, selectedGem.y).GetValue();
        gem.transform.DOScale(Vector3.one * 0.2f, 0.2f).SetEase(Ease.OutBounce);

        // Trocar os valores na grade
        grid.SetValue(gridPosA.x, gridPosA.y, gridObjectB);
        grid.SetValue(gridPosB.x, gridPosB.y, gridObjectA);

        yield return new WaitForSeconds(0.5f);
    }

    //Initializes the grid
    void InitializeGrid()
    {
        grid = GridSystem2D<GridObject<Gem>>.VerticalGrid(width, height, cellSize, originPosition, debug);

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                CreateGem(x, y);
            }
        }
    }

   //Create gems on the grid, assuring that there is no matches on start
    void CreateGem(int x, int y)
    {
        var gemType = GetRandomGemType(x, y);

        var gem = Instantiate(gemPrefab, grid.GetWorldPositionCenter(x, y), Quaternion.identity, transform);
        gem.SetType(gemType);

        var gridObject = new GridObject<Gem>(grid, x, y);
        gridObject.SetValue(gem);

        grid.SetValue(x, y, gridObject);
    }

    //Gets a random gem that is not equal to the adjacent ones
    GemType GetRandomGemType(int x, int y)
    {
        GemType newGemType;
        do
        {
            // Escolhe aleatoriamente um tipo de gema
            newGemType = gemTypes[Random.Range(0, gemTypes.Length)];

        } while (IsAdjacentGemTypeSame(x, y, newGemType));

        return newGemType;
    }

    //Checks adjacent gems to assure that there are no matches on start
    bool IsAdjacentGemTypeSame(int x, int y, GemType newGemType)
    {
        if (y > 0 && grid.GetValue(x, y - 1)?.GetValue()?.GetType() == newGemType)
        {
            return true;
        }

        if (x > 0 && grid.GetValue(x - 1, y)?.GetValue()?.GetType() == newGemType)
        {
            return true;
        }

        if (x < width - 1 && grid.GetValue(x + 1, y)?.GetValue()?.GetType() == newGemType)
        {
            return true;
        }

        if (y < height - 1 && grid.GetValue(x, y + 1)?.GetValue()?.GetType() == newGemType)
        {
            return true;
        }

        return false;
    }



    //Deselects the gem when the player clicks a selected gem
    void DeselectGem()
    {
        if (selectedGem == Vector2Int.one * -1) return;

        // Desfaz a escala da gema selecionada
        var gem = grid.GetValue(selectedGem.x, selectedGem.y).GetValue();
        gem.transform.DOScale(Vector3.one * 0.2f, 0.2f).SetEase(Ease.OutBounce);

        // Reseta a seleção
        selectedGem = Vector2Int.one * -1;
    }

    //Selects a gem
    void SelectGem(Vector2Int gridPos)
    {
        // Primeiro, verifica se há uma gema já selecionada e a desfaz
        if (selectedGem != Vector2Int.one * -1)
        {
            DeselectGem();
        }

        // Agora, seleciona a nova gema
        selectedGem = gridPos;

        // Aplica um efeito de escala na gema selecionada
        var gem = grid.GetValue(selectedGem.x, selectedGem.y).GetValue();
        gem.transform.DOScale(Vector3.one * 0.25f, 0.2f).SetEase(Ease.OutBounce);
    }


    bool IsEmptyPosition(Vector2Int gridPosition) => grid.GetValue(gridPosition.x, gridPosition.y) == null;

    bool IsValidPosition(Vector2 gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < width && gridPosition.y >= 0 && gridPosition.y < height;
    }
}