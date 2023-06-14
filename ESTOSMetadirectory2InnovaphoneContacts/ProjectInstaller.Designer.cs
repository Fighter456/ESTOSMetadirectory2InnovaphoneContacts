namespace ESTOSMetadirectory2InnovaphoneContacts
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Komponenten-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceProcessInstaller1 = new System.ServiceProcess.ServiceProcessInstaller();
            this.ESTOSMetadirectory2InnovaphoneContactsInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceProcessInstaller1
            // 
            this.serviceProcessInstaller1.Account= System.ServiceProcess.ServiceAccount.LocalSystem;
            // 
            // ESTOSMetadirectory2InnovaphoneContactsInstaller
            // 
            this.ESTOSMetadirectory2InnovaphoneContactsInstaller.Description = "Converts the CSV exports from ESTOS Metadirectory for usage with Innovaphone PBX." +
    "";
            this.ESTOSMetadirectory2InnovaphoneContactsInstaller.DisplayName = "ESTOSMetadirectory2InnovaphoneContacts";
            this.ESTOSMetadirectory2InnovaphoneContactsInstaller.ServiceName = "ESTOSMetadirectory2InnovaphoneContacts";
            this.ESTOSMetadirectory2InnovaphoneContactsInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcessInstaller1,
            this.ESTOSMetadirectory2InnovaphoneContactsInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller1;
        private System.ServiceProcess.ServiceInstaller ESTOSMetadirectory2InnovaphoneContactsInstaller;
    }
}