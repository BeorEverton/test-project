using UnityEngine;

public class Recoil : MonoBehaviour
{
    [SerializeField] private Transform _barrel;
    private Vector3 _barrelOriginalLocalPos;
    private float _recoilTimer = 0f;
    [SerializeField] private float _recoilDuration = 0.1f;
    [SerializeField] private float _recoilDistance = 0.15f;

    private void Start()
    {
        _barrelOriginalLocalPos = _barrel.localPosition;
    }

    protected void Update()
    {
        ApplyBarrelRecoil();
    }

    public void ApplyBarrelRecoil()
    {
        if (!(_recoilTimer > 0f))
            return;

        _recoilTimer -= Time.deltaTime;

        float time = 1f - (_recoilTimer / _recoilDuration);
        _barrel.localPosition = Vector3.Lerp(
            _barrelOriginalLocalPos + Vector3.up * _recoilDistance,
            _barrelOriginalLocalPos,
            time
        );
    }

    public void AddRecoil()
    {
        _recoilTimer = _recoilDuration;
        _barrel.localPosition = _barrelOriginalLocalPos + Vector3.up * _recoilDistance;
    }
}
