using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Skillbridge.Utilities
{
    /// <summary>
    /// Lida com o toast
    /// </summary>
    public static class ToastHelper
    {
        /// <summary>
        /// Método esta estático para mostrar o toast
        /// </summary>
        /// <param name="tempData"></param>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="type"></param>
        public static void ShowToast(ITempDataDictionary tempData, string title, string message, string type)
        {
            tempData["ToastTitle"] = title;
            tempData["ToastMessage"] = message;
            tempData["ToastType"] = type; // success, danger, warning, info
        }
    }
}