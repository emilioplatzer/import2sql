/*
 * Creado por SharpDevelop.
 * Usuario: Emilio
 * Fecha: 04/05/2008
 * Hora: 12:52
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Data;

using Comunes;
using BasesDatos;

namespace ModeladorSql
{
	public class Ejecutador:BasesDatos.EjecutadorSql{
		public static Bitacora bitacora=new Bitacora("pr_query.sql","pr_queries.sql");
		ListaCampos CamposContexto=new ListaCampos();
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
			base.ExecuteNonQuery(Obtener(laSentencia));
		}
		public IDataReader EjecutarReader(Sentencia laSentencia){
			return base.ExecuteReader(Obtener(laSentencia));
		}
		public string Obtener(Sentencia laSentencia){
			foreach(Tabla t in laSentencia.Tablas(QueTablas.Aliasables).Keys){
				// bitacora.Registrar("Tabla alias "+t.NombreTabla+","+t.Alias+","+t.AliasActual);
				t.AliasActual=t.Alias;
				if(!t.LiberadaDelContextoDelEjecutador){
					t.CamposContexto=CamposContexto;
				}
			}
			return bitacora.RegistrarSql(laSentencia.ToSql(db)+";\n");
		}
		public string Dump(Sentencia laSentencia){
			string obtenido=Obtener(laSentencia);
			db.CompliarParaControlar(obtenido);
			return obtenido;
		}
		public void AssertSinRegistros(string explicacion,Sentencia laSentencia){
			db.AssertSinRegistros(explicacion,Obtener(laSentencia));
		}
	}
}
