using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLogViewer.ClientApplication.Models;
using NLogViewer.ClientApplication.Parsers;

namespace NLogViewer.ClientApplication.Services;

/// <summary>
/// Service for receiving UDP log messages and parsing them
/// </summary>
public class UdpLogReceiverService(Log4JEventParser xmlParser) : IDisposable
{
	private readonly List<UdpClient> _udpClients = new();
	private readonly List<CancellationTokenSource> _cancellationTokens = new();
	private readonly Log4JEventParser _xmlParser = xmlParser ?? throw new ArgumentNullException(nameof(xmlParser));
	private bool _disposed;

	public IObservable<Log4JEvent> Log4JEventObservable => _log4JEventObservable;
	private readonly Subject<Log4JEvent> _log4JEventObservable = new();

	public void StartListening(List<string> addresses)
	{
		StopListening();

		foreach (var address in addresses)
		{
			try
			{
				var uri = new Uri(address);
				if (uri.Scheme != "udp")
					continue;

				var port = uri.Port;
				var udpClient = new UdpClient(port);
				_udpClients.Add(udpClient);

				var cts = new CancellationTokenSource();
				_cancellationTokens.Add(cts);

				// Start receiving on this port
				Task.Run(() => ReceiveLoop(udpClient, port, cts.Token), cts.Token);
			}
			catch (Exception ex)
			{
				// Log error but continue with other ports
				System.Diagnostics.Debug.WriteLine($"Error starting listener on {address}: {ex.Message}");
			}
		}
	}

	public void StopListening()
	{
		foreach (var cts in _cancellationTokens)
		{
			cts.Cancel();
		}
		_cancellationTokens.Clear();

		foreach (var client in _udpClients)
		{
			try
			{
				client.Close();
			}
			catch
			{
				// Ignore errors during cleanup
			}
		}
		_udpClients.Clear();
	}

	private async Task ReceiveLoop(UdpClient client, int port, CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				var result = await client.ReceiveAsync();
				var data = result.Buffer;
				var sender = result.RemoteEndPoint?.ToString();

				// Parse the received data
				var xml = Encoding.UTF8.GetString(data);
				var logEvent = _xmlParser.Parse(xml);

				if (logEvent != null)
				{
					_log4JEventObservable.OnNext(logEvent);
				}
			}
			catch (ObjectDisposedException)
			{
				// Client was disposed, exit loop
				break;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Error receiving UDP data on port {port}: {ex.Message}");
				// Continue receiving despite errors
			}
		}
	}
	
	public void Dispose()
	{
		if (!_disposed)
		{
			StopListening();
			_log4JEventObservable.Dispose();
			_disposed = true;
		}
	}
}