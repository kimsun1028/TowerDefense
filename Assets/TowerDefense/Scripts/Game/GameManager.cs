using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {get; private set; }

    [SerializeField] private int startGold = 1300;
    [SerializeField] private int startLives = 20;

    public int Gold {get; private set;}
    public int Lives {get; private set;}
    public bool IsGameOver{get; private set;}

    public event Action<int> GoldChanged;
    public event Action<int> LivesChanged;
    public event Action GameWon;
    public event Action GameLost;

    private void Awake()
    {   
        this.Gold = startGold;
        this.Lives = startLives;
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        GoldChanged?.Invoke(Gold);
        LivesChanged?.Invoke(Lives);
    }

    private void OnEnable()
    {
        Enemy.Killed += OnEnemyKilled;
        Enemy.ReachedGoal += OnEnemyReachedGoal;
        WaveSpawner.AllWavesCleared += OnAllWavesCleared;
    }

    private void OnDisable()
    {
        Enemy.Killed -= OnEnemyKilled;
        Enemy.ReachedGoal -= OnEnemyReachedGoal;
        WaveSpawner.AllWavesCleared -= OnAllWavesCleared;
    }

    // 건설/업그레이드에서 호출 : 충분하면 차감 후 true;
    public bool TrySpendGold(int amount)
    {
        if(Gold >= amount)
        {
            Gold -= amount;
            GoldChanged?.Invoke(Gold);
            return true;
        }
        return false;
    }

    // 판매 환급 등
    public void AddGold(int amount)
    {
        Gold += amount;
        GoldChanged?.Invoke(Gold);
    }

    private void OnEnemyKilled(Enemy enemy)
    {
        AddGold(enemy.Data.GoldReward);
    }

    private void OnEnemyReachedGoal(Enemy enemy)
    {
        if(IsGameOver)
        {
            return;
        }
        Lives = Mathf.Max(0, Lives - enemy.Data.LifeDamage);
        LivesChanged?.Invoke(Lives);
        if(Lives <= 0)
        {
            IsGameOver = true;
            GameLost?.Invoke();
        }
    }

    private void OnAllWavesCleared()
    {
        if (!IsGameOver)
        {
            IsGameOver = true;
            GameWon?.Invoke();
        }
    }


}
