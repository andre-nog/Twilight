using System.Linq;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class RelayManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField joinInput;
    [SerializeField] private TextMeshProUGUI codeText;

    [Header("Relay")]
    [SerializeField] private int maxConnections = 3;   // nº de clients (fora o host)
    [SerializeField] private string protocol = "dtls"; // "dtls" recomendado; pode usar "udp" se quiser sem DTLS

    private bool _initialized;

    private async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            _initialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Relay] Falha ao inicializar/autorizar: {e.Message}");
            return;
        }

        if (hostButton) hostButton.onClick.AddListener(CreateRelay);
        if (joinButton) joinButton.onClick.AddListener(() => JoinRelay(joinInput ? joinInput.text : string.Empty));
    }

    private void OnDestroy()
    {
        if (hostButton) hostButton.onClick.RemoveListener(CreateRelay);
        if (joinButton) joinButton.onClick.RemoveAllListeners();
    }

    private async void CreateRelay()
    {
        if (!_initialized) return;

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            if (codeText) codeText.text = $"Code: {joinCode}";
            Debug.Log($"[Relay] JoinCode: {joinCode}");

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            // Pega um endpoint que case com o ConnectionType pedido (dtls ou udp); senão, pega o primeiro
            var endpoint =
                allocation.ServerEndpoints.FirstOrDefault(e => e.ConnectionType == protocol)
                ?? allocation.ServerEndpoints.First();

            string host = endpoint != null ? endpoint.Host : allocation.RelayServer.IpV4;
            ushort port = (ushort)(endpoint != null ? endpoint.Port : allocation.RelayServer.Port);

            // Host usa sua própria ConnectionData como HostConnectionData
            transport.SetRelayServerData(
                host,
                port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.ConnectionData,
                protocol == "dtls" // secure/DTLS se for dtls
            );

            if (!NetworkManager.Singleton.StartHost())
                Debug.LogError("[Relay] Falha ao iniciar Host.");
            else
                Debug.Log("[Relay] Host iniciado. Aguardando clients…");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay] Erro ao criar relay: {e.Message}");
        }
    }

    private async void JoinRelay(string joinCode)
    {
        if (!_initialized) return;
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogWarning("[Relay] Join code vazio.");
            return;
        }

        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim());

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            var endpoint =
                joinAllocation.ServerEndpoints.FirstOrDefault(e => e.ConnectionType == protocol)
                ?? joinAllocation.ServerEndpoints.First();

            string host = endpoint != null ? endpoint.Host : joinAllocation.RelayServer.IpV4;
            ushort port = (ushort)(endpoint != null ? endpoint.Port : joinAllocation.RelayServer.Port);

            transport.SetRelayServerData(
                host,
                port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData,
                protocol == "dtls"
            );

            if (!NetworkManager.Singleton.StartClient())
                Debug.LogError("[Relay] Falha ao iniciar Client.");
            else
                Debug.Log("[Relay] Client conectando via Relay…");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay] Erro ao entrar no relay: {e.Message}");
        }
    }
}
