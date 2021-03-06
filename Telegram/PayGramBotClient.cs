﻿using log4net;
using Newtonsoft.Json;
using PayGram.Public;
using PayGram.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PayGram.Public.Telegram
{
	public class PayGramBotClient
	{
		static private readonly ILog log = LogManager.GetLogger(typeof(PayGramBotClient));
		static readonly HttpClient _httpClient = new HttpClient();

		public Guid Token;

		public PayGramBotClient(Guid token)
		{
			Token = token;
		}

		/// <summary>
		/// Sends the money directly from ones account to someone else's account
		/// </summary>
		/// <param name="toTid">The id of the person to send the funds to</param>
		/// <param name="amount">The amount to send</param>
		/// <param name="curSym">The currency symbol, any of the <see cref="CurrenciesLib.Currencies"/> .ToString()</param>
		/// <returns>True if it succeded, false otherwise</returns>
		public async Task<bool> TransferMoneyAsync(int toTid, double amount, string curSym = PayGramHelper.PAYGRAM_SYMBOL)
		{
			if (amount <= 0) return false;
			string response = await ExecuteMethodAsync(Token, PayGramHelper.TRANSFER_METHOD, $"&cursym={curSym}&amt={amount.ToString(CultureInfo.InvariantCulture)}&to={toTid}", null);
			log.Debug($"transferring {amount} to tid {toTid}, response: {response}");
			if (response == null)
			{
				log.Error($"error transferring {amount} to tid {toTid}, PayGram bot returned null");
				return false;
			}
			try
			{
				var res = JsonConvert.DeserializeObject<PaygramResponse>(response);
				return res.Success;
			}
			catch (Exception e)
			{
				log.Error($"error transferring {amount} to tid {toTid}", e);
			}
			return false;
		}

		/// <summary>
		/// Asks PayGram to generate a crypto currency address where to send funds. when funds arrive, it will be sent to paygram
		/// </summary>
		/// <param name="usdAmount">The amount to deposit, in usd</param>
		/// <param name="amountCurrency">The crypto currency symbol and network, ie: BTC, USDT_ERC20, USDT_TRC20, LTC</param>
		/// <param name="callbackData">The data to receive on a callback when the user has deposited</param>
		/// <returns>ResponseTopUp or ResponseTopUpCryptapi</returns> 
		public async Task<ResponseTopUp> DepositCreditAsync(double amount, string amountCurrency, string payCurrency, string callbackData)
		{
			if (amount <= 0) return new ResponseTopUp() { Success = false, Message = "Amount cannot be negative" };
			string response = await ExecuteMethodAsync(Token, PayGramHelper.DEPOSIT_METHOD, $"&{PayGramHelper.CURRENCY_SYMBOL_TOKEN_NAME}={amountCurrency}&{PayGramHelper.AMT_TOKEN_NAME}={amount.ToString(CultureInfo.InvariantCulture)}&{PayGramHelper.PAY_CURRENCY_SYMBOL_TOKEN_NAME}={payCurrency}&{PayGramHelper.CALLBACKDATA_TOKEN_NAME}={callbackData}", null);
			log.Debug($"DepositMoneyAsync {amount} {amountCurrency}, response: {response}");
			if (string.IsNullOrWhiteSpace(response))
			{
				log.Error($"error DepositCreditAsync {amount} {amountCurrency}, PayGram bot returned null");
				return new ResponseTopUp() { Success = false, Message = "Unexpected error" }; ;
			}
			try
			{
				var res = JsonConvert.DeserializeObject<ResponseTopUp>(response);
				return res.Type == PaygramResponseTypes.ResponseTopUpCryptapi ? JsonConvert.DeserializeObject<ResponseTopUpCryptapi>(response) : res;
			}
			catch (Exception e)
			{
				log.Error($"error DepositCreditAsync {amount} {amountCurrency}", e);
				return new ResponseTopUp() { Success = false, Message = "Unexpected error" }; ;
			}
		}

		/// <summary>
		/// Converts an amount from srcCurr to destCurr
		/// </summary>
		/// <param name="srcCurr"></param>
		/// <param name="destCurr"></param>
		/// <param name="amount"></param>
		/// <returns>The converted amount or decimal.MinValue in case of error</returns>
		public async Task<decimal> Convert(string srcCurr, string destCurr, decimal amount)
		{
			string response = await ExecuteMethodAsync(Token, PayGramHelper.CONVERT_METHOD, $"&{PayGramHelper.AMOUNT_TAG}={amount}&{PayGramHelper.CURRENCY_SYMBOL_TOKEN_NAME}={srcCurr}&{PayGramHelper.CURRENCY_SYMBOL_DEST_TOKEN_NAME}={destCurr}", null);
			if (response == null)
				return decimal.MinValue;
			decimal result;
			if (decimal.TryParse(response, out result))
				return result;
			return decimal.MinValue;
		}

		public async Task<ResponseGetUpdates> GetUpdatesAsync()
		{
			string response = await ExecuteMethodAsync(Token, PayGramHelper.UPDATES_METHOD, null, null);
			if (response == null)
				return new ResponseGetUpdates() { Success = false };

			return JsonConvert.DeserializeObject<ResponseGetUpdates>(response);
		}

		public async Task<ResponseGetExchangeRates> GetExchangeRatesAsync()
		{
			string response = await ExecuteMethodAsync(Token, PayGramHelper.EXCHANGE_RATES_METHOD, null, null);
			if (response == null)
				return new ResponseGetExchangeRates() { Success = false };

			return JsonConvert.DeserializeObject<ResponseGetExchangeRates>(response);
		}
		/// <summary>
		/// Gets the information about the passed invoice
		/// </summary>
		/// <param name="invoiceId">The invoice id whose info is needed</param>
		/// <returns>A ResponseInvoiceInfo or ResponseInvoiceWithdrawInfo depending on the type of the invoice. Does not return null.</returns>
		public async Task<ResponseInvoiceInfo> GetInvoiceInfo(Guid invoiceId)
		{
			string response = await ExecuteMethodAsync(Token, PayGramHelper.INVOICE_INFO_METHOD, $"&{PayGramHelper.INVOICEID_TOKEN_NAME}={invoiceId}", null);
			if (string.IsNullOrWhiteSpace(response))
				return new ResponseInvoiceInfo() { Success = false };

			var resp = JsonConvert.DeserializeObject<ResponseInvoiceInfo>(response);

			if (resp == null)
			{
				log.Warn($"{invoiceId} info returned null");
				return new ResponseInvoiceInfo() { Success = false, Message = "Unexpected error" };
			}

			if (resp.Type == PaygramResponseTypes.ResponseInvoiceWithdrawInfo)
				return JsonConvert.DeserializeObject<ResponseInvoiceWithdrawInfo>(response);
			else
				return resp;
		}

		/// <summary>
		/// Sends a request to the PayGram bot server. Returns null in case of errors with the communication with the server.
		/// </summary>
		/// <param name="rq">the query string</param>
		/// <param name="method">the body request if any</param>
		/// <returns></returns>
		async static Task<string> ExecuteMethodAsync(Guid token, string method, string otherParams, object request)
		{
			if (otherParams == null)
				otherParams = "";
			if (otherParams != "" && otherParams.StartsWith("&") == false)
				otherParams = $"&{otherParams}";

			string url = string.Format(PayGramHelper.PAYGRAM_BOT_ENDPOINT, token, method, otherParams);

			string reqContent = request != null ? JsonConvert.SerializeObject(request) : "";

			//log.Debug($"Requesting: {url}, {reqContent}");

			using (HttpRequestMessage m = new HttpRequestMessage(HttpMethod.Post, url))
			{
				using (HttpContent cont = new StringContent(reqContent, Encoding.UTF8, "application/json"))
				{
					m.Content = cont;
					try
					{
						using (HttpResponseMessage res = await _httpClient.SendAsync(m))
						{
							if (res.IsSuccessStatusCode == false)
							{
								return null;
							}

							string resContent = await res.Content.ReadAsStringAsync();

							return resContent;
						}
					}
					catch (Exception ex)
					{
						log.Debug($"Error sending the request to the server. {url} {reqContent}", ex);
						return null;
					}
				}
			}
		}
	}
}
