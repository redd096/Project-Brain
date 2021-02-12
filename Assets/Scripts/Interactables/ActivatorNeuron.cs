﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivatorNeuron : Neuron
{
    [Header("Activator")]
    [SerializeField] Activable[] objectsToActivate = default;

    public override void ActiveInteractable(bool active)
    {
        base.ActiveInteractable(active);

        //foreach object in the list, try activate or deactivate
        foreach (Activable activable in objectsToActivate)
            activable.ToggleObject(this, active);
    }
}
