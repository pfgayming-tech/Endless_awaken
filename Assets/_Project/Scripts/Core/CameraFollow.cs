using UnityEngine;

namespace VSL
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smooth = 10f;

        private void LateUpdate()
        {
            if (target == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) target = p.transform;
                else return;
            }

            Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * smooth);
        }
    }
}
