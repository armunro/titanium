namespace Titanium.Samples;

public class ReceiptSamples
{
    public static string RECEIPT_MODEL_GPT =
        $$"""
          {
            "store": {
              "name": String,
              "address": String,
              "phone": String,
            },
            "transaction": {
              "date": "06/18/24",
              "time": "17:80:68"
            },
            "items": [
              {"name": "ITEM1", "sku": "000000045670", "price": 0.33}, 
              {"name": "ITEM2", "sku": "085912006030", "price": 2.97},
              {"name": "ITEM3", "sku": "628916830770", "price": 2.47},
              {"name": "ITEM4", "sku": "628915226160", "price": 10.00},
              {"name": "ITEM...", "sku": "6289185666260", "price": 11.97},
              
            ],
            "totals": {
              "subtotal": 63.21,
              "taxes": {
                "GST": 0.84,
                "P&T": 1.01
              },
              "total": 65.06
            },
            "payment": {
              "method": "debit",
              "amount_tendered": 65.06,
              "account_number": "**** **** **** 8732",
            }
          }

          """;
}