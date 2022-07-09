provider "azurerm" {
  features {
    
  }
}

data "azurerm_client_config" "current" {}

resource "azuread_application" "this" {
  display_name     = "${var.name}-${var.stage}"
  owners           = [data.azurerm_client_config.current.object_id]
  sign_in_audience = "AzureADMultipleOrgs"
}

resource "azuread_application_password" "this" {
  application_object_id = azuread_application.this.object_id
}

resource "azurerm_resource_group" "this" {
  name     = "rg-${var.name}-${var.stage}"
  location = "West Europe"
}

resource "azurerm_bot_service_azure_bot" "this" {
  name                = "bot-${var.name}-${var.stage}"
  resource_group_name = azurerm_resource_group.this.name
  location            = "global"
  microsoft_app_id    = azuread_application.this.application_id
  sku                 = "F0"

  developer_app_insights_api_key        = azurerm_application_insights_api_key.this.api_key
  developer_app_insights_application_id = azurerm_application_insights.this.app_id
}

resource "azurerm_bot_channel_ms_teams" "this" {
  bot_name            = azurerm_bot_service_azure_bot.this.name
  location            = azurerm_bot_service_azure_bot.this.location
  resource_group_name = azurerm_resource_group.this.name
}

resource "azurerm_application_insights" "this" {
  name                = "appi-${var.name}-${var.stage}"
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name
  application_type    = "web"
  retention_in_days = 30
}

resource "azurerm_application_insights_api_key" "this" {
  name                    = "bot"
  application_insights_id = azurerm_application_insights.this.id
  read_permissions        = ["aggregate", "api", "draft", "extendqueries", "search"]
}

resource "azurerm_service_plan" "this" {
  name                = "plan-${var.name}-${var.stage}"
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  os_type             = "Linux"
  sku_name            = "Y1"
}

resource "azurerm_linux_function_app" "this" {
  name                = "func-${var.name}-${var.stage}"
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  storage_account_name = azurerm_storage_account.this.name

  service_plan_id      = azurerm_service_plan.this.id

  https_only = true

  app_settings = {
    MS_APP_ID = azuread_application.this.application_id
    MS_APP_PASSWORD = azuread_application_password.this.value
  }

  site_config {
    application_insights_connection_string = azurerm_application_insights.this.connection_string
  }
}

resource "azurerm_storage_account" "this" {
  name                     = "st${lower(var.name)}${var.stage}"
  resource_group_name      = azurerm_resource_group.this.name
  location                 = azurerm_resource_group.this.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}