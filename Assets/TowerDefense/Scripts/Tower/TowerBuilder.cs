using UnityEngine;

public class TowerBuilder : MonoBehaviour
{
    [SerializeField] private Tower[] towerPrefabs;   // 0 Archer, 1 Magic, 2 Artillery

    private TowerSlot selectedSlot;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SelectSlot();
        }

        // 임시: UI 붙기 전 테스트용 단축키
        if (Input.GetKeyDown(KeyCode.Alpha1)) Build(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) Build(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) Build(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) Build(3);
    }

    private void SelectSlot()
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        selectedSlot = null;
        Collider2D[] colliders = Physics2D.OverlapPointAll(worldPos);
        foreach(Collider2D other in colliders)
        {
            if(other.TryGetComponent(out TowerSlot towerslot) && !towerslot.IsOccupied)
            {
                selectedSlot = towerslot;
                break;
            }
            else
            {
                selectedSlot = null;
            }
        }
    }

    // UI 버튼도 이 함수를 호출하게 될 예정
    public void Build(int index)
    {
        // 1. selectedSlot이 null이거나 이미 지어졌으면 return
        if(selectedSlot == null || selectedSlot.IsOccupied)
        {
            return;
        }

        // 2. 프리팹 생성: 위치는 슬롯 위치, 회전 없음, 부모는 슬롯
        //    Instantiate(towerPrefabs[index], 위치, Quaternion.identity, 부모)
        Tower tower = Instantiate(towerPrefabs[index], selectedSlot.transform.position, Quaternion.identity, selectedSlot.transform);

        // 3. tower.Init(selectedSlot);
        //    selectedSlot.SetOccupant(tower);
        tower.Init(selectedSlot);
        selectedSlot.SetOccupant(tower);

        // 4. selectedSlot = null
        selectedSlot = null;
    }
}
