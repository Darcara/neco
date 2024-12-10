namespace Neco.Common.Data.Web;

using System;
using System.Net.Http;

public static class KnownClientNames {
	/// <summary>
	/// The default handler/client configuration that is used if no (or null) name is given during construction of a new <see cref="SocketsHttpHandler"/> or <see cref="HttpClient"/> 
	/// </summary>
	public const String Default = "";

	/// <summary>
	/// Handler/client configuration that is always invoked, on any created handler or client, before name-specific configurators will be applied
	/// </summary>
	public const String Always = "161FA440-7C82-4C84-9794-7176F3FC1FAE";
}