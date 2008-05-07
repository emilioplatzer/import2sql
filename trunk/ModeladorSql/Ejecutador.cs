/*
 * Creado por SharpDevelop.
 * Usuario: Emilio
 * Fecha: 04/05/2008
 * Hora: 12:52
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;

using Comunes;
using BasesDatos;

namespace ModeladorSql
{
	public class Ejecutador:BasesDatos.EjecutadorSql{
		public static Bitacora bitacora=new Bitacora("pr_query.sql","pr_queries.sql");
		ListaElementos<Campo> CamposContexto=new ListaElementos<Campo>();
		public Ejecutador(BaseDatos db,params Tabla[] TablasContexto)
			:base(db)
		{
			foreach(Tabla t in TablasContexto){
				foreach(Campo c in t.CamposPk()){
					if(c.ValorSinTipo!=null){
						CamposContexto.Add(c);
					}
				}
			}
		}
		public void Ejecutar(Sentencia laSentencia){
			base.ExecuteNonQuery(Dump(laSentencia));
		}
		public string Dump(Sentencia laSentencia){
			Console.WriteLine("Por hacer un Dump:");
			foreach(Tabla t in laSentencia.Tablas(QueTablas.Aliasables).Keys){
				Console.WriteLine("Tabla {0} alias {1},{2}",t.NombreTabla,t.Alias,t.AliasActual);
				bitacora.Registrar("Tabla alias "+t.NombreTabla+","+t.Alias+","+t.AliasActual);
				t.AliasActual=t.Alias;
				if(!t.LiberadaDelContextoDelEjecutador){
					t.CamposContexto=CamposContexto;
				}
			}
			return bitacora.RegistrarSql(laSentencia.ToSql(db)+";\n");
		}
	}
}
