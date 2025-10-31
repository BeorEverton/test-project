namespace Assets.Scripts.Turrets
{
    public class MachineGunTurret : BaseTurret
    {
        // NOT USED ANYMORE, CRITICAL HIT LOGIC MOVED TO BASETURRET
        /*

        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            base.Update();
        }

        protected override void Shoot()
        {
            base.Shoot();


            float finalDamage = RuntimeStats.Damage;
            bool isCritical = IsCriticalHit();

            if (isCritical)
            {
                AudioManager.Instance.Play("Critical");
                finalDamage *= 1f + (RuntimeStats.CriticalDamageMultiplier / 100f);
            }

            _targetEnemy.GetComponent<Enemy>().TakeDamage(finalDamage, isCritical);

            StatsManager.Instance.AddTurretDamage(_turretInfo.TurretType, finalDamage);
            _timeSinceLastShot = 0f;
        }

        private bool IsCriticalHit()
        {
            return Random.Range(0, 100) < RuntimeStats.CriticalChance;
        }
        */
    }
}