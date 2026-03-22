using System;
using UnityEngine;

public abstract class EntityBase : MonoBehaviour, IReference
{
    public GlobalManager GM => GlobalManager.Instance;

    public string id;
    public int instanceId;
    public bool ownedByPlayer;
    public bool isActive = true;
    private void Start()
    {
        OnStart();
    }
    private void OnDestroy()
    {
        // OnRecycle();
    }


    public virtual void OnStart() { }
    // public virtual void OnSpawn() {OnStart(); isActive = true;GlobalManager.Instance.entityManager.Register(this);}
    public virtual void OnSpawn() { }
    public virtual void OnUpdate(float deltaTime) { }
    public virtual void OnTick(float tickTime) {}
    public virtual void OnRecycle() { }
}