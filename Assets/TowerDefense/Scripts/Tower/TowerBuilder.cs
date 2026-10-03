using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class TowerBuilder : MonoBehaviour
{
    [SerializeField] private Tower[] towerPrefabs;   // 0 Archer, 1 Magic, 2 Artillery, 3 Barracks, 4 Hero

    private TowerSlot selectedSlot;

    // UI 연동 API (약속문 기준)
    public event Action<TowerSlot> SlotSelected;   // 빈 슬롯 클릭
    public event Action<Tower> TowerSelected;      // 지어진 타워 클릭
    public event Action SelectionCleared;          // 빈 땅 클릭 / 건설 완료

    public int GetBuildCost(int index)
    {
        return towerPrefabs[index].Data.costs[0];
    }

    public bool CanBuild(int index)
    {
        return GameManager.Instance.Gold >= GetBuildCost(index);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }

        // 임시: UI 붙기 전 테스트용 단축키
        if (Input.GetKeyDown(KeyCode.Alpha1)) Build(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Build(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Build(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) Build(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) Build(4);

        // 링 메뉴가 버튼(강화/판매 등)으로 닫히면 사거리 표시도 끔
        if (shownTower != null && RingMenu.Instance != null && !RingMenu.Instance.IsOpen)
        {
            ShowTowerRange(null);
        }
    }

    // 선택한 타워의 사거리 타원만 보이게 (null이면 모두 끔)
    private Tower shownTower;
    private void ShowTowerRange(Tower tower)
    {
        if (shownTower != null && shownTower != tower) shownTower.ShowRange(false);
        shownTower = tower;
        if (tower != null) tower.ShowRange(true);
    }

    private void HandleClick()
    {
        // UI 위 클릭은 UI가 처리
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 집결지 지정 중이면 클릭을 넘기고 끝 (도구가 스스로 종료됨)
        if (RallyPointTool.Instance != null && RallyPointTool.Instance.IsActive)
        {
            RallyPointTool.Instance.HandleClick(worldPos);
            return;
        }

        SelectAt(worldPos);
    }

    private void SelectAt(Vector2 worldPos)
    {
        // 타워 사거리 트리거도 같이 잡히므로 전부 확인해서 슬롯을 찾음
        TowerSlot clickedSlot = null;
        foreach (Collider2D other in Physics2D.OverlapPointAll(worldPos))
        {
            if (other.TryGetComponent(out TowerSlot slot))
            {
                clickedSlot = slot;
                break;
            }
        }

        if (clickedSlot == null)
        {
            ClearSelection();
        }
        else if (!clickedSlot.IsOccupied)
        {
            selectedSlot = clickedSlot;
            ShowTowerRange(null);
            SlotSelected?.Invoke(clickedSlot);
        }
        else
        {
            selectedSlot = null;
            Tower tower = clickedSlot.Occupant as Tower;
            ShowTowerRange(tower);
            TowerSelected?.Invoke(tower);
        }
    }

    private void ClearSelection()
    {
        selectedSlot = null;
        ShowTowerRange(null);
        SelectionCleared?.Invoke();
    }

    public void Build(int index)
    {
        if (selectedSlot == null || selectedSlot.IsOccupied)
        {
            return;
        }

        if (!GameManager.Instance.TrySpendGold(GetBuildCost(index)))
        {
            return;
        }

        Tower tower = Instantiate(towerPrefabs[index], selectedSlot.transform.position, Quaternion.identity, selectedSlot.transform);
        tower.Init(selectedSlot);
        selectedSlot.SetOccupant(tower);

        ClearSelection();
    }
}
