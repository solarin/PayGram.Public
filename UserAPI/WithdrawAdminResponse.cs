﻿namespace PayGram.Public.UserAPI
{
	public class WithdrawAdminResponse
	{
		/// <summary>
		/// Can be set by an admin, can be read by admins
		/// </summary>
		public string AdminComment { get; set; }
		/// <summary>
		/// Can be set by admins, can be read by authorized user/client
		/// </summary>
		public string AdminResponse { get; set; }
		/// <summary>
		/// The status of the WithdrawInvoice. Setting this to InvoiceStatuses.Cancelled will not refund the user. 
		/// To Cancel and refund the user, set CancelWithdraw to true on RequestUpdateWithdraw
		/// Requested = 1,
		/// Completed = 2,
		/// Cancelled = 3,
		/// OnGoing = 4,
		/// Error = 5,
		/// Denied = 7,
		/// AcceptedPendingTransfer = 8
		/// </summary>
		public int InvoiceStatus { get; set; }
		/// <summary>
		/// The amount effectively transferred
		/// When an admin replies, if the status is complete, AmountSent=0 and CurrencyCode=null, the amount will be taken from the originating invoice
		/// </summary>
		public decimal AmountSent { get; set; }
		/// <summary>
		/// The currency code of AmountSent
		/// It is the string representing one of the <see cref="Currencies"/>.
		/// If the CurrencyCode represents a crypto currency it might be followed by the <see cref="Crypto.CRYPTO_NETWORK_SEPARATOR"/> and the network name 
		/// which is one of <see cref="CryptoCurrencies"/> for example USDT_ERC20
		/// When an admin replies, if the status is complete, AmountSent=0 and CurrencyCode=null, the CurrencyCode will be taken from the originating invoice
		/// </summary>
		public string CurrencyCode { get; set; }
		public WithdrawMethod UpdatedWithdrawMethod { get; set; }
	}
}
