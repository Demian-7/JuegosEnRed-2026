// using Fusion;
// using Unity.VisualScripting;
// using UnityEngine;
// public class PlayerController : NetworkBehaviour
// {
//     [SerializeField] private float _speed = 5f;
//     [SerializeField] private NetworkPrefabRef _bulletPrefab;
//
//     public override void FixedUpdateNetwork()
//     {
//         if (GetInput(out NetworkInputData data))
//         {
//             Vector3 movement = new Vector3(data.Direction.x, data.Direction.y, 0);
//
//             transform.Translate(movement * _speed * Runner.DeltaTime);
//
//             if (data.Buttons.IsSet((int)InputButton.Shoot))
//             {
//                 //Logica de disparo
//                 if (Object.HasStateAuthority) 
//                 {
//                     Runner.Spawn(_bulletPrefab, transform.position, Quaternion.identity);
//                 }
//             }
//         }
//     }
// }
