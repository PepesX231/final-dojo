using UnityEngine;

namespace DreamRush.Dream
{
    public interface IPushable
    {
        void Push(Vector3 direction, float dt, Transform pusher);
    }
}
