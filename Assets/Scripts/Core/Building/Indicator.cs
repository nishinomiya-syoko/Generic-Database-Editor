using UnityEngine;
using DG.Tweening;

namespace Top
{
    public class Indicator:EntityBase
    {
        [SerializeField]private GameObject rangeIndcicator;
        [SerializeField]private GameObject sizeIndicator;
        private Vector2Int m_size;
        private float m_range;

        public void SetSize(Vector2Int size,float range=0)
        {
            m_size = size;
            m_range = range;
            // if (rangeIndcicator == null)
            // {
            //     rangeIndcicator = GlobalManager.Instance.poolManager.Spawn("RangeIndicator", transform.position);
            //     rangeIndcicator.transform.SetParent(transform);
            //     rangeIndcicator.SetActive(false);

            //     sizeIndicator = GlobalManager.Instance.poolManager.Spawn("SizeIndicator", transform.position);
            //     sizeIndicator.transform.SetParent(transform);
            //     sizeIndicator.SetActive(false);
            // }
        }

        public void DoSize()
        {
            sizeIndicator.SetActive(true);
            sizeIndicator.transform.localScale = new Vector3(m_size.x, 1, m_size.y);
            // sizeIndicator.transform.DOScale(new Vector3(m_size.x, 1, m_size.y), 0.5f).SetEase(Ease.OutBack);
            rangeIndcicator.SetActive(true);
            rangeIndcicator.transform.DOScale(new Vector3(m_range, 1, m_range), 0.5f).SetEase(Ease.OutBack);
        }
        public void SetPosition(Vector3 pos)
        {
            transform.position = pos;
        }
    }
}
