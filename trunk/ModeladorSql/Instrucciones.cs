/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 03/05/2008
 * Time: 01:20 p.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Text;

using Comunes;
using BasesDatos;

namespace ModeladorSql
{
	public class Instruccion:IElemento{
		protected ListaElementos<ElementoTipado<bool>> ClausulaWhere;
		public virtual string ToSql(BaseDatos db){
			Falla.Detener("No implementado aún");
			return null;
		}
		public virtual ConjuntoTablas Tablas(QueTablas queTablas){
			return new ConjuntoTablas();
		}
	}
	public class ElementosClausulaSelect:ListaElementos<IConCampos>{}
	public class ElementosClausula:ListaElementos<IElementoTipado<bool>>,IElemento{
		
		public string ToSql(BaseDatos db){
			StringBuilder rta=new StringBuilder();
			Separador and=new Separador("\n AND ");
			foreach(IElementoTipado<bool> e in this){
				rta.Append(and+e.ToSql(db));
			}
			return rta.ToString();
		}
		public ConjuntoTablas Tablas(QueTablas queTablas){
			ConjuntoTablas rta=new ConjuntoTablas();
			foreach(IElementoTipado<bool> e in this){
				rta.AddRange(e.Tablas(queTablas));
			}
			return rta;
		}
	}
	public class ElementosClausulaWhere:ElementosClausula{}
	public class InstruccionSelect:Instruccion{
		public ElementosClausulaSelect ClausulaSelect;
	}
	public class InstruccionInsert:Instruccion{
		protected Tabla TablaBase;
		protected InstruccionSelect InstruccionSelectBase;
		public override string ToSql(BaseDatos db){
			StringBuilder rta=new StringBuilder();
			rta.Append("INSERT INTO ");
			rta.Append(TablaBase.ToSql(db));
			Separador coma=new Separador(" (",", ");
			InstruccionSelect SelectFiltrado=InstruccionSelectBase;
			SelectFiltrado.ClausulaSelect=new ElementosClausulaSelect();
			foreach(IConCampos e in InstruccionSelectBase.ClausulaSelect){
				foreach(Campo c in e.Campos()){
					if(TablaBase.ContieneMismoNombre(c)){
						SelectFiltrado.ClausulaSelect.Add(c);
						rta.Append(coma+db.StuffCampo(c.NombreCampo));
					}
				}
			}
			rta.Append(") ");
			return rta.ToString();
		}
	}
	public class InstruccionUpdate:Instruccion{
		// ListaElementos<IElementoReNombrable> CamposUpdate;
	}
}
