using TMPro;
using UnityEngine;

/// <summary>
/// Top bar: wood, food, gold, population (used / capacity) and age of the local player.
/// Reads the [Networked] values of the local NetworkPlayerState; every client sees only its own numbers.
/// </summary>
public class HudUI : MonoBehaviour
{
    [SerializeField] TMP_Text wood, food, gold, population, age;

    NetworkPlayerState state;

    void Update()
    {
        if (state == null)
        {
            if (CommandRelay.Local == null) return;
            state = CommandRelay.Local.GetComponent<NetworkPlayerState>();
            if (state == null) return;
        }
        if (wood) wood.text = "Wood " + state.Wood;
        if (food) food.text = "Food " + state.Food;
        if (gold) gold.text = "Gold " + state.Gold;
        if (population) population.text = "Pop " + state.PopUsed + "/" + state.PopCapacity;
        if (age) age.text = "Age " + (state.Age == 2 ? "II" : "I");
    }
}
