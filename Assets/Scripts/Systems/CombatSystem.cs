using System;

public struct CombatSimulationResult
{
    public int estimatedHeroHealth;
    public int estimatedEnemyHealth;
    public int totalHeroDamageTaken;
    public int totalEnemyDamageTaken;
}

public static class CombatSystem
{
    public static int CalculateDamage(int attackValue, int defenseValue)
    {
        return Math.Max(1, attackValue - defenseValue);
    }

    public static CombatSimulationResult Simulate(PlayerGridMovement hero, Enemy enemy)
    {
        return Simulate(hero.CurrentHealth, hero.Attack, hero.Defense, hero.CombatSpeed, enemy.CurrentHealth, enemy.Attack, enemy.Defense, enemy.Speed);
    }

    public static CombatSimulationResult Simulate(int heroHealth, int heroAttack, int heroDefense, float heroSpeed, int enemyHealth, int enemyAttack, int enemyDefense, float enemySpeed)
    {
        CombatSimulationResult result = new CombatSimulationResult
        {
            estimatedHeroHealth = heroHealth,
            estimatedEnemyHealth = enemyHealth
        };

        if (heroHealth <= 0 || enemyHealth <= 0)
            return result;

        float heroNextAttackTime = 1f / Math.Max(0.1f, heroSpeed);
        float enemyNextAttackTime = 1f / Math.Max(0.1f, enemySpeed);
        int heroDamage = CalculateDamage(heroAttack, enemyDefense);
        int enemyDamage = CalculateDamage(enemyAttack, heroDefense);

        while (result.estimatedHeroHealth > 0 && result.estimatedEnemyHealth > 0)
        {
            bool heroActsFirst = heroNextAttackTime <= enemyNextAttackTime;

            if (heroActsFirst)
            {
                result.estimatedEnemyHealth -= heroDamage;
                result.totalEnemyDamageTaken += heroDamage;
                heroNextAttackTime += 1f / Math.Max(0.1f, heroSpeed);
                if (result.estimatedEnemyHealth <= 0)
                    break;
            }
            else
            {
                result.estimatedHeroHealth -= enemyDamage;
                result.totalHeroDamageTaken += enemyDamage;
                enemyNextAttackTime += 1f / Math.Max(0.1f, enemySpeed);
                if (result.estimatedHeroHealth <= 0)
                    break;
            }
        }

        result.estimatedHeroHealth = Math.Max(0, result.estimatedHeroHealth);
        result.estimatedEnemyHealth = Math.Max(0, result.estimatedEnemyHealth);
        return result;
    }

    public static CombatSimulationResult ResolveMelee(PlayerGridMovement hero, Enemy enemy)
    {
        CombatSimulationResult simulation = Simulate(hero, enemy);

        int heroDamageTaken = Math.Max(0, hero.CurrentHealth - simulation.estimatedHeroHealth);
        int enemyDamageTaken = Math.Max(0, enemy.CurrentHealth - simulation.estimatedEnemyHealth);

        if (enemyDamageTaken > 0)
            enemy.ApplyDirectDamage(enemyDamageTaken);

        if (heroDamageTaken > 0)
            hero.ApplyDirectDamage(heroDamageTaken);

        return simulation;
    }

    public static int ResolveRangedAttack(Enemy enemy, PlayerGridMovement hero)
    {
        int damage = CalculateDamage(enemy.Attack, hero.Defense);
        hero.ApplyDirectDamage(damage);
        return damage;
    }
}
