﻿using System;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using Zyan.Communication.Toolbox;
using Zyan.SafeDeserializationHelpers.Channels;

namespace Zyan.Communication.Protocols.Tcp
{
	/// <summary>
	/// Client protocol setup for TCP communication with support for user defined authentication and security.
	/// </summary>
	public sealed class TcpCustomClientProtocolSetup : CustomClientProtocolSetup, IClientProtocolSetup
	{
		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		public TcpCustomClientProtocolSetup(Versioning versioning)
			: base((settings, clientSinkChain, serverSinkChain) => new TcpChannel(settings, clientSinkChain, serverSinkChain))
		{
			SocketCachingEnabled = true;
			_channelName = "TcpCustomClientProtocolSetup" + Guid.NewGuid().ToString();
			_versioning = versioning;

			Hashtable formatterSettings = new Hashtable();
			formatterSettings.Add("includeVersions", _versioning == Versioning.Strict);
			formatterSettings.Add("strictBinding", _versioning == Versioning.Strict);

			ClientSinkChain.Add(new SafeBinaryClientFormatterSinkProvider(formatterSettings, null));
			ServerSinkChain.Add(new SafeBinaryServerFormatterSinkProvider(formatterSettings, null) { TypeFilterLevel = TypeFilterLevel.Full });
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		public TcpCustomClientProtocolSetup()
			: this(Versioning.Strict)
		{ }

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		public TcpCustomClientProtocolSetup(bool encryption)
			: this()
		{
			Encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		public TcpCustomClientProtocolSetup(Versioning versioning, bool encryption)
			: this(versioning)
		{
			Encryption = encryption;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		public TcpCustomClientProtocolSetup(bool encryption, string algorithm)
			: this()
		{
			Encryption = encryption;
			Algorithm = algorithm;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		public TcpCustomClientProtocolSetup(Versioning versioning, bool encryption, string algorithm)
			: this(versioning)
		{
			Encryption = encryption;
			Algorithm = algorithm;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		public TcpCustomClientProtocolSetup(bool encryption, string algorithm, int maxAttempts)
			: this()
		{
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		public TcpCustomClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, int maxAttempts)
			: this(versioning)
		{
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="oaep">Specifies if OAEP padding should be used</param>
		public TcpCustomClientProtocolSetup(bool encryption, string algorithm, bool oaep)
			: this()
		{
			Encryption = encryption;
			Algorithm = algorithm;
			Oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="oaep">Specifies if OAEP padding should be used</param>
		public TcpCustomClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, bool oaep)
			: this(versioning)
		{
			Encryption = encryption;
			Algorithm = algorithm;
			Oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		/// <param name="oaep">Specifies if OAEP padding should be used</param>
		public TcpCustomClientProtocolSetup(bool encryption, string algorithm, int maxAttempts, bool oaep)
			: this()
		{
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
			Oaep = oaep;
		}

		/// <summary>
		/// Creates a new instance of the TcpCustomClientProtocolSetup class.
		/// </summary>
		/// <param name="versioning">Versioning behavior</param>
		/// <param name="encryption">Specifies if the communication sould be encrypted</param>
		/// <param name="algorithm">Encryption algorithm (e.G. "3DES")</param>
		/// <param name="maxAttempts">Maximum number of connection attempts</param>
		/// <param name="oaep">Specifies if OAEP padding should be used</param>
		public TcpCustomClientProtocolSetup(Versioning versioning, bool encryption, string algorithm, int maxAttempts, bool oaep)
			: this(versioning)
		{
			Encryption = encryption;
			Algorithm = algorithm;
			MaxAttempts = maxAttempts;
			Oaep = oaep;
		}

		/// <summary>
		/// Gets or sets, if sockets should be cached and reused.
		/// <remarks>
		/// Caching sockets may reduce ressource consumption but may cause trouble in Network Load Balancing clusters.
		/// </remarks>
		/// </summary>
		public bool SocketCachingEnabled { get; set; }

		/// <summary>
		/// Formats the connection URL for this protocol.
		/// </summary>
		/// <param name="serverAddress">The server address.</param>
		/// <param name="portNumber">The port number.</param>
		/// <param name="zyanHostName">Name of the zyan host.</param>
		/// <returns>
		/// Formatted URL supported by the protocol.
		/// </returns>
		public string FormatUrl(string serverAddress, int portNumber, string zyanHostName)
		{
			return (this as IClientProtocolSetup).FormatUrl(serverAddress, portNumber, zyanHostName);
		}

		/// <summary>
		/// Formats the connection URL for this protocol.
		/// </summary>
		/// <param name="parts">The parts of the url, such as server name, port, etc.</param>
		/// <returns>
		/// Formatted URL supported by the protocol.
		/// </returns>
		string IClientProtocolSetup.FormatUrl(params object[] parts)
		{
			if (parts == null || parts.Length < 3)
				throw new ArgumentException(GetType().Name + " requires three arguments for URL: server address, port number and ZyanHost name.");

			return string.Format("tcp://{0}:{1}/{2}", parts);
		}

		/// <summary>
		/// Checks whether the given URL is valid for this protocol.
		/// </summary>
		/// <param name="url">The URL to check.</param>
		/// <returns>
		/// True, if the URL is supported by the protocol, otherwise, False.
		/// </returns>
		public override bool IsUrlValid(string url)
		{
			return base.IsUrlValid(url) && url.StartsWith("tcp");
		}

		/// <summary>
		/// Creates and configures a Remoting channel.
		/// </summary>
		/// <returns>Remoting channel</returns>
		public override IChannel CreateChannel()
		{
			IChannel channel = ChannelServices.GetChannel(_channelName);

			if (channel == null)
			{
				_channelSettings["name"] = _channelName;
				_channelSettings["port"] = 0;
				_channelSettings["socketCacheTimeout"] = 0;
				_channelSettings["socketCachePolicy"] = SocketCachingEnabled ? SocketCachePolicy.Default : SocketCachePolicy.AbsoluteTimeout;
				_channelSettings["secure"] = false;

				ConfigureEncryption();
				ConfigureCompression();

				if (_channelFactory == null)
					throw new ApplicationException(LanguageResource.ApplicationException_NoChannelFactorySpecified);

				channel = _channelFactory(_channelSettings, BuildClientSinkChain(), BuildServerSinkChain());
				RemotingHelper.ResetCustomErrorsMode();
			}

			return channel;
		}

		#region Versioning settings

		private Versioning _versioning = Versioning.Strict;

		/// <summary>
		/// Gets or sets the versioning behavior.
		/// </summary>
		private Versioning Versioning
		{
			get { return _versioning; }
		}

		#endregion
	}
}
