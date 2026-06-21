using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Skillbridge.Utilities
{
    public static class ToastHelper
    {
        // Método estático que você vai chamar no seu código
        public static void ShowToast(ITempDataDictionary tempData, string title, string message, string type)
        {
            tempData["ToastTitle"] = title;
            tempData["ToastMessage"] = message;
            tempData["ToastType"] = type; // success, danger, warning, info
        }
    }
}