/*
 * Creado por SharpDevelop.
 * Usuario: Emilio
 * Fecha: 04/05/2008
 * Hora: 12:56
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificaci�n | Editar Encabezados Est�ndar
 */

using System;

using Comunes;
using BasesDatos;

namespace ModeladorSql
{
	public abstract class Sentencia{
		Instruccion instruccion;
		protected Sentencia(Instruccion instruccion){
			// S� o s� hay que crear una instrucci�n y pasar el puntero ac�
			this.instruccion=instruccion;
		}
		public virtual string ToSql(BaseDatos db){
			return instruccion.ToSql(db);
		}
		
	}
	public class SentenciaInsert:Sentencia{
		InstruccionInsert instruccionInsert;
		public SentenciaInsert(Tabla TablaBase)
			:base(instruccionInsert=new InstruccionInsert())
		{
			instruccionInsert.TablaBase=TablaBase;
		}
		public SentenciaInsert Select(params IConCampos[] conCampos){
			instruccionInsert.InstruccionSelectBase.ClausulaSelect=conCampos;
		}
				
	}
}
