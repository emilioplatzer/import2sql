/*
 * Creado por SharpDevelop.
 * Usuario: eplatzer
 * Fecha: 04/06/2008
 * Hora: 12:33
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Data;
using System.Data.Odbc;
using System.Drawing;
using System.Windows.Forms;

using Comunes;
using BasesDatos;
using ModeladorSql;
using Indices;

namespace Interactivo
{
	public class GrillaBaseDatosUnaTabla:Form{
		private BaseDatos db;
		private Tabla tabla;
		private IDbConnection con;
		private IDbDataAdapter da;
		private DataSet  ds;
		private DataGrid dg;
		private Button btnUpdate;
		public GrillaBaseDatosUnaTabla(BaseDatos db,Tabla tabla){
			this.db=db;
			this.tabla=tabla;
			Initdata();
			dg=new DataGrid();
			dg.Location=new Point(5, 5);
			dg.DataSource=ds;
			dg.DataMember=tabla.NombreTabla;
			btnUpdate=new Button();
			btnUpdate.Text="Actualizar";
			AdaptarCambioTamanno();
			btnUpdate.Click+=new EventHandler(btnUpdateClicked);
			SizeChanged+=new EventHandler(formSizeChanged);
			Controls.AddRange(new Control[] { dg, btnUpdate });
		}
		public void AdaptarCambioTamanno(){
			dg.Size=new Size(this.ClientRectangle.Size.Width - 10,this.ClientRectangle.Height - 50);
			btnUpdate.Location=new Point(
				this.ClientRectangle.Width/2 - btnUpdate.Width/2,
				this.ClientRectangle.Height - (btnUpdate.Height + 10));
		}
		public void Initdata(){
			con=db.con;
			ds=new DataSet();
			if(con is OdbcConnection){
				var con_odbc=con as OdbcConnection;
				// da=new OdbcDataAdapter("select * from "+db.StuffTabla(tabla.NombreTabla), con_odbc);
				da=new OdbcDataAdapter("select producto,nombreproducto from productos", con_odbc);
				var da_odbc=da as OdbcDataAdapter;
				OdbcCommandBuilder cmdBldr = new OdbcCommandBuilder(da_odbc);
				Console.WriteLine(cmdBldr.GetInsertCommand().CommandText);
				Console.WriteLine(cmdBldr.GetUpdateCommand().CommandText);
				Console.WriteLine(cmdBldr.GetDeleteCommand().CommandText);
				da_odbc.Fill(ds,tabla.NombreTabla);
			}
		}
		public void btnUpdateClicked(object sender, EventArgs e){
			if(con is OdbcConnection){
				var con_odbc=con as OdbcConnection;
				var da_odbc=da as OdbcDataAdapter;
				da_odbc.Update(ds,tabla.NombreTabla);
			}
		}
		public void formSizeChanged(object sender, EventArgs e){
			AdaptarCambioTamanno();
		}
	}
	public class GrillaBaseDatos:Form{
		private BaseDatos db;
		private ConjuntoTablas tablas=new ConjuntoTablas();
		private IDbConnection con;
		private IDbDataAdapter[] da;
		private DataSet  ds;
		private DataGrid dg;
		private Button btnUpdate;
		public GrillaBaseDatos(BaseDatos db,params Tabla[] tablas){
			this.db=db;
			this.tablas.AddRange(tablas);
			da=new IDbDataAdapter[tablas.Length];
			Initdata();
			dg=new DataGrid();
			dg.Location=new Point(5, 5);
			dg.DataSource=ds;
			// dg.DataMember=tabla.NombreTabla;
			btnUpdate=new Button();
			btnUpdate.Text="Actualizar";
			AdaptarCambioTamanno();
			btnUpdate.Click+=new EventHandler(btnUpdateClicked);
			SizeChanged+=new EventHandler(formSizeChanged);
			Controls.AddRange(new Control[] { dg, btnUpdate });
		}
		public void AdaptarCambioTamanno(){
			dg.Size=new Size(this.ClientRectangle.Size.Width - 10,this.ClientRectangle.Height - 50);
			btnUpdate.Location=new Point(
				this.ClientRectangle.Width/2 - btnUpdate.Width/2,
				this.ClientRectangle.Height - (btnUpdate.Height + 10));
		}
		public void Initdata(){
			con=db.con;
			ds=new DataSet();
			if(con is OdbcConnection){
				var con_odbc=con as OdbcConnection;
				// da=new OdbcDataAdapter("select * from "+db.StuffTabla(tabla.NombreTabla), con_odbc);
				int i=0;
				foreach(var tabla in tablas.Keys){
					da[i]=new OdbcDataAdapter("select * from "+db.StuffTabla(tabla.NombreTabla), con_odbc);
					var da_odbc=da[i] as OdbcDataAdapter;
					OdbcCommandBuilder cmdBldr = new OdbcCommandBuilder(da_odbc);
					Console.WriteLine(cmdBldr.GetInsertCommand().CommandText);
					Console.WriteLine(cmdBldr.GetUpdateCommand().CommandText);
					Console.WriteLine(cmdBldr.GetDeleteCommand().CommandText);
					da_odbc.Fill(ds,tabla.NombreTabla);
					i++;
				}
				foreach(Tabla tabla in tablas.Keys){
					if(tablas.Contiene(tabla.TablaRelacionada)){
						Tabla hija=tabla.TablaRelacionada;
						var campost=new System.Collections.Generic.List<DataColumn>();
						var camposh=new System.Collections.Generic.List<DataColumn>();
						foreach(var par in tabla.CamposRelacionFk){
							campost.Add(ds.Tables[tabla.NombreTabla].Columns[par.Key.NombreCampo]);
							camposh.Add(ds.Tables[hija.NombreTabla].Columns[(par.Value as Campo).NombreCampo]);
						}
						/*
						Console.WriteLine("{0} {1} {2} {3} {4} {5}",
							tabla.NombreTabla+"_"+hija.NombreTabla
							,Objeto.ExpandirMiembros(campost.ToArray())
							,Objeto.ExpandirMiembros(camposh.ToArray())
							,false
						);
						*/
						var dr=new DataRelation(
							hija.NombreTabla+" de "+tabla.NombreTabla
							,campost.ToArray()
							,camposh.ToArray()
							,false
						);
						Falla.SiEsNulo(dr);
						Falla.SiEsNulo(ds.Relations);
						ds.Relations.Add(dr);
					}
				}
			}
		}
		public void btnUpdateClicked(object sender, EventArgs e){
			if(con is OdbcConnection){
				var con_odbc=con as OdbcConnection;
				int i=0;
				foreach(var tabla in tablas.Keys){
					var da_odbc=da[i] as OdbcDataAdapter;
					da_odbc.Update(ds,tabla.NombreTabla);
					i++;
				}
			}
		}
		public void formSizeChanged(object sender, EventArgs e){
			AdaptarCambioTamanno();
		}
	}
	public class PruebasInteractivas{
		public void ProbarGrilla(){
			BaseDatos dbp=PostgreSql.Abrir("127.0.0.1","import2sqldb","import2sql","sqlimport");
			var especificaciones=new Especificaciones();
			especificaciones.UsarFk();
			var productos=especificaciones.fkProductos;
			Application.Run(new GrillaBaseDatos(dbp,productos,especificaciones));
		}
	}
}
