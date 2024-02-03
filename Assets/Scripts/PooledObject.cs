using System;
using System.Collections;
using System.Collections.Generic;
using Ilumisoft.SkillDrive;
using UnityEngine;

namespace DesignPatterns.ObjectPool
{
    public class PooledObject : MonoBehaviour
    {
        private ObjectPool pool;

        private GameObject owner;
        
        private Projectile projectile;
        
        private Rigidbody rb;
        
        public ObjectPool Pool { get => pool; set => pool = value; }
        
        public GameObject Owner { get => owner; set => owner = value; }

        public Projectile Projectile { get => projectile;   set => projectile = value;}

        public Rigidbody Rb { get => rb; set => rb = value; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            projectile = GetComponent<Projectile>();
        }

        public void Release()
        {
            pool.ReturnToPool(this);
        }
    }
}
