using UnityEngine;
using Zenject;

public class PrizeEvaluatorInstaller : MonoInstaller
{
    [SerializeField] private ScoreBasedPrizeEvaluator prizeEvaluator;

    public override void InstallBindings()
    {
        Container.Bind<ScoreBasedPrizeEvaluator>()
            .FromInstance(prizeEvaluator)
            .AsSingle()
            .NonLazy();
    }
}
