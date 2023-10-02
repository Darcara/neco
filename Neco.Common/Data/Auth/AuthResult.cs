namespace Neco.Common.Data.Auth; 

public enum AuthResult {
	// No authentication check has been done yet
	Unknown = 0,
	/// Authentication has failed
	Failed,
	/// Authentication was successful
	Authenticated,
}